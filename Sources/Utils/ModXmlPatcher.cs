/* MIT License

Copyright (c) 2022 OCB7D2D

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

static class ModXmlPatcher
{

    // Must be set from outside first, otherwise not much happens
    public static Dictionary<string, Func<bool>> Conditions = null;

    // Evaluates one single condition (can be negated)
    private static bool EvaluateCondition(string condition)
    {
        // Try to get optional condition from global dictionary
        if (Conditions != null && Conditions.TryGetValue(condition, out Func<bool> callback))
        {
            // Just call the function
            // We don't cache anything
            return callback();
        }
        // Otherwise check if a mod with that name exists
        // ToDo: maybe do something with ModInfo.version?
        else if (ModManager.GetMod(condition) != null)
        {
            return true;
        }
        // Otherwise it's false
        // Unknown tests too
        return false;
    }

    // Evaluate a comma separated list of conditions
    // The results are logically `and'ed` together
    private static bool EvaluateConditions(string conditions, XmlFile xml)
    {
        // Ignore if condition is empty or null
        if (string.IsNullOrEmpty(conditions)) return false;
        // Split comma separated list (no whitespace allowed yet)

        if (conditions.StartsWith("xpath:"))
        {
            conditions = conditions.Substring(6);
            // Use negative look-behind to allow comma escape sequence
            // Still very bad semantics, but better than not allowing it
            foreach (string rawpath in Regex.Split(conditions, "(?<!\\\\),"))
            {
                bool negate = false;
                var xpath = rawpath.Replace("\\,", ",");
                List<System.Xml.Linq.XElement> xmlNodeList;
                if (xpath.StartsWith("!"))
                {
                    negate = true;
                    xmlNodeList = xml.XmlDoc.XPathSelectElements(
                        xpath.Substring(1)).ToList();
                }
                else
                {
                    xmlNodeList = xml.XmlDoc.XPathSelectElements(xpath).ToList();
                }
                bool result = true;
                if (xmlNodeList == null) result = false;
                if (xmlNodeList.Count == 0) result = false;
                if (negate) result = !result;
                if (!result) return false;
            }
        }
        else
        {
            foreach (string condition in conditions.Split(','))
            {
                bool result = true;
                // Try to find version comparator
                int notpos = condition[0] == '!' ? 1 : 0;
                int ltpos = condition.IndexOf("<");
                int gtpos = condition.IndexOf(">");
                int lepos = condition.IndexOf("≤");
                int gepos = condition.IndexOf("≥");
                int length = condition.Length - notpos;
                if (ltpos != -1) length = ltpos - notpos;
                else if (gtpos != -1) length = gtpos - notpos;
                else if (lepos != -1) length = lepos - notpos;
                else if (gepos != -1) length = gepos - notpos;
                string name = condition.Substring(notpos, length);
                if (length != condition.Length - notpos)
                {
                    if (ModManager.GetMod(name) is Mod mod)
                    {
                        string version = condition.Substring(notpos + length + 1);
                        Version having = mod.Version;
                        Version testing = Version.Parse(version);
                        if (ltpos != -1) result = having < testing;
                        if (gtpos != -1) result = having > testing;
                        if (lepos != -1) result = having <= testing;
                        if (gepos != -1) result = having >= testing;
                    }
                    else
                    {
                        result = false;
                    }
                }
                else if (!EvaluateCondition(name))
                {
                    result = false;
                }

                if (notpos == 1) result = !result;
                if (result == false) return false;
            }
        }

        // Something was true
        return true;
    }

    // Static dictionary for templating
    static Dictionary<string, string> dict =
        new Dictionary<string, string>();

    // Helper class to keep correct template dict scoping
    // Add new keys when initialized, restores old on close
    private class DictScopeHelper : IDisposable
    {
        Dictionary<string, string> prev = null;
        public DictScopeHelper(XElement element) =>
            dict = GetTemplateValues(prev = dict, element);
        public void Dispose() => dict = prev;
    }

    // Add placeholder keys to our template dictionary
    private static Dictionary<string, string> GetTemplateValues(
        Dictionary<string, string> parent, XElement child)
    {
        // Make a copy of the parent dictionary
        Dictionary<string, string> dict =
            new Dictionary<string, string>(parent);
        // Add new template values to dictionary
        foreach (var attr in child.Attributes())
        {
            if (!attr.Name.LocalName.StartsWith("tmpl-")) continue;
            dict[attr.Name.LocalName.Substring(5)] = attr.Value;
        }
        // Return new dict
        return dict;
    }

    // Replace all placeholder the dictionary contains
    private static void ReplaceTemplateOccurences(
        Dictionary<string, string> dict, ref string text)
    {
        foreach (var kv in dict)
            text = text.Replace(
                "{{" + kv.Key + "}}",
                kv.Value);
    }

    // Function to load another XML file and basically call the same PatchXML function again
    private static bool IncludeAnotherDocument(XmlFile target, XmlFile parent, XElement element, Mod mod)
    {
        bool result = true;

        var modName = mod.Name;

        // Add template values to dictionary
        using (var tmpls = new DictScopeHelper(element))
        foreach (XAttribute attr in element.Attributes())
        {
            // Skip unknown attributes
            if (attr.Name != "path") continue;
            // Load path relative to previous XML include
            string prev = Path.Combine(parent.Directory, parent.Filename);
            string path = Path.Combine(Path.GetDirectoryName(prev), attr.Value);
            if (File.Exists(path))
            {
                try
                {
                    string _text = File.ReadAllText(path, Encoding.UTF8)
                        .Replace("@modfolder:", "@modfolder(" + modName + "):");
                    ReplaceTemplateOccurences(dict, ref _text);
                    XmlFile _patchXml;
                    try
                    {
                        _patchXml = new XmlFile(_text,
                            Path.GetDirectoryName(path),
                            Path.GetFileName(path),
                            true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("XML loader: Loading XML patch include '{0}' from mod '{1}' failed.", path, modName);
                        Log.Exception(ex);
                        result = false;
                        continue;
                    }
                    result &= XmlPatcher.PatchXml(
                        target, _patchXml.XmlDoc.Root, _patchXml, mod);
                }
                catch (Exception ex)
                {
                    Log.Error("XML loader: Patching '" + target.Filename + "' from mod '" + modName + "' failed.");
                    Log.Exception(ex);
                    result = false;
                }
            }
            else
            {
                Log.Error("XML loader: Can't find XML include '{0}' from mod '{1}'.", path, modName);
            }
        }
        return result;
    }

    // Basically the same function as `XmlPatcher.PatchXml`
    // Patched to support `include` and `modif` XML elements

    static int count = 0;

    public static bool PatchXml(XmlFile xmlFile,
        XmlFile patchXml, XElement node, Mod mod)
    {
        bool result = true;
        count++;
        ParserStack stack = new ParserStack();
        stack.count = count;
        foreach (XElement child in node.Elements())
        {
            // Patched to support includes
            if (child.Name == "modinc")
            {
                // Will do the magic by calling our functions again
                IncludeAnotherDocument(xmlFile, patchXml, child, mod);
            }
            else if (child.Name == "echo")
            {
                foreach (XAttribute attr in child.Attributes())
                {
                    if (attr.Name == "log") Log.Out("{1}: {0}", attr.Value, xmlFile.Filename);
                    if (attr.Name == "warn") Log.Warning("{1}: {0}", attr.Value, xmlFile.Filename);
                    if (attr.Name == "error") Log.Error("{1}: {0}", attr.Value, xmlFile.Filename);
                    if (attr.Name != "log" && attr.Name != "warn" && attr.Name != "error")
                        Log.Warning("Echo has no valid name (log, warn or error)");
                }
            }
            // Otherwise try to apply the patches found in child element
            else if (!ApplyPatchEntry(xmlFile, patchXml, child, mod, ref stack))
            {
                IXmlLineInfo lineInfo = child;
                Log.Warning(string.Format("XML patch for \"{0}\" from mod \"{1}\" did not apply: {2} (line {3} at pos {4})",
                    xmlFile.Filename, mod.Name, child.ToString(), lineInfo.LineNumber, lineInfo.LinePosition));
                result = false;
            }
        }
        return result;
    }

    // Flags for consecutive mod-if parsing
    public struct ParserStack
    {
        public int count;
        public bool IfClauseParsed;
        public bool PreviousResult;
    }

    // Entry point instead of (private) `XmlPatcher.singlePatch`
    // Implements conditional patching and also allows includes
    private static bool ApplyPatchEntry(XmlFile _targetFile, XmlFile _patchFile,
        XElement _patchElement, Mod _patchingMod, ref ParserStack stack)
    {

        // Only support root level
        switch (_patchElement.Name.ToString())
        {

            case "modinc":

                // Call out to our include handler
                return IncludeAnotherDocument(_targetFile, _patchFile,
                    _patchElement, _patchingMod);

            case "modif":

                // Reset flags first
                stack.IfClauseParsed = true;
                stack.PreviousResult = false;

                // Check if we have true conditions
                foreach (XAttribute attr in _patchElement.Attributes())
                {
                    // Ignore unknown attributes for now
                    if (attr.Name != "condition")
                    {
                        Log.Warning("Ignoring unknown attribute {0}", attr.Name);
                        continue;
                    }
                    // Evaluate one or'ed condition
                    if (EvaluateConditions(attr.Value, _targetFile))
                    {
                        stack.PreviousResult = true;
                        return PatchXml(_targetFile, _patchFile,
                            _patchElement, _patchingMod);
                    }
                }

                // Nothing failed!?
                return true;

            case "modelsif":

                // Check for correct parser state
                if (!stack.IfClauseParsed)
                {
                    Log.Error("Found <modelsif> clause out of order");
                    return false;
                }

                // Abort else when last result was true
                if (stack.PreviousResult) return true;

                // Check if we have true conditions
                foreach (XAttribute attr in _patchElement.Attributes())
                {
                    // Ignore unknown attributes for now
                    if (attr.Name != "condition")
                    {
                        Log.Warning("Ignoring unknown attribute {0}", attr.Name);
                        continue;
                    }
                    // Evaluate one or'ed condition
                    if (EvaluateConditions(attr.Value, _targetFile))
                    {
                        stack.PreviousResult = true;
                        return PatchXml(_targetFile, _patchFile,
                            _patchElement, _patchingMod);
                    }
                }

                // Nothing failed!?
                return true;

            case "modelse":

                // Reset flags first
                stack.IfClauseParsed = false;
                // Abort else when last result was true
                if (stack.PreviousResult) return true;
                return PatchXml(_targetFile, _patchFile,
                    _patchElement, _patchingMod);

            default:
                // Reset flags first
                stack.IfClauseParsed = false;
                stack.PreviousResult = true;
                // Dispatch to original function
                return XmlPatcher.singlePatch(_targetFile,
                    _patchElement, _patchFile, _patchingMod);
        }
    }

    // Hook into vanilla XML Patcher
    [HarmonyPatch(typeof(XmlPatcher))]
    [HarmonyPatch("PatchXml")]
    public class XmlPatcher_PatchXml
    {
        static bool Prefix(
            ref XmlFile _xmlFile,
            ref XmlFile _patchFile,
            XElement _containerElement,
            ref Mod _patchingMod,
            ref bool __result)
        {
            // According to Harmony docs, returning false on a prefix
            // should skip the original and all other prefixers, but
            // it seems that it only skips the original. The other
            // prefixers are still called. The reason for this is
            // unknown, but could be because the game uses HarmonyX.
            // Might also be something solved with latest versions,
            // as the game uses a rather old HarmonyX version (2.2).
            // To address this we simply "consume" one of the args.
            if (_patchFile == null) return false;
            XElement element = _patchFile.XmlDoc.Root;
            if (element == null) return false;
            string version = element.GetAttribute("patcher-version");
            if (!string.IsNullOrEmpty(version))
            {
                // Check if version is too new for us
                if (int.Parse(version) > 7) return true;
            }
            // Call out to static helper function
            __result = PatchXml(_xmlFile, _patchFile,
                _containerElement, _patchingMod);
            // First one wins
            _patchFile = null;
            return false;
        }
    }

}
