using UnityEngine;

public class XUiC_OcbPowerSourceStats : XUiController
{

    public XUiC_OcbPowerSourceWindowGroup Owner { get; set; }

    private Color32 onColor = new Color32((byte)250, byte.MaxValue, (byte)163, byte.MaxValue);

    private Color32 offColor = (Color32)Color.white;

    private XUiController btnToggleSolarCharge;

    private XUiV_Button btnToggleSolarChargeBG;

    private XUiV_Label lblToggleSolarCharge;

    private XUiV_Sprite sprToggleSolarCharge;

    private bool chargeFromSolar;

    private bool btnToggleSolarChargeHovered;

    private XUiController btnToggleGeneratorCharge;

    private XUiV_Button btnToggleGeneratorChargeBG;

    private XUiV_Label lblToggleGeneratorCharge;

    private XUiV_Sprite sprToggleGeneratorCharge;

    private bool chargeFromGenerator;

    private bool btnToggleGeneratorChargeHovered;

    private XUiController btnToggleBatteryCharge;

    private XUiV_Button btnToggleBatteryChargeBG;

    private XUiV_Label lblToggleBatteryCharge;

    private XUiV_Sprite sprToggleBatteryCharge;

    private bool chargeFromBattery;

    private bool btnToggleBatteryChargeHovered;

    private string turnOff;

    private string turnOn;

    public override void Init()
    {
        base.Init();

        btnToggleSolarChargeHovered = false;
        btnToggleSolarCharge = GetChildById("btnToggleSolarCharge");
        btnToggleSolarChargeBG = (XUiV_Button)btnToggleSolarCharge.GetChildById("clickable").ViewComponent;
        btnToggleSolarChargeBG.Controller.OnPress += new XUiEvent_OnPressEventHandler(BtnToggleSolarCharge_OnPress);
        btnToggleSolarChargeBG.Controller.OnHover += new XUiEvent_OnHoverEventHandler(BtnToggleSolarCharge_OnHover);
        XUiController childById1 = GetChildById("lblToggleSolarCharge");
        if (childById1 != null) lblToggleSolarCharge = (XUiV_Label)childById1.ViewComponent;
        XUiController childById2 = GetChildById("sprToggleSolarCharge");
        if (childById2 != null) sprToggleSolarCharge = (XUiV_Sprite)childById2.ViewComponent;

        btnToggleGeneratorChargeHovered = false;
        btnToggleGeneratorCharge = GetChildById("btnToggleGeneratorCharge");
        btnToggleGeneratorChargeBG = (XUiV_Button)btnToggleGeneratorCharge.GetChildById("clickable").ViewComponent;
        btnToggleGeneratorChargeBG.Controller.OnPress += new XUiEvent_OnPressEventHandler(BtnToggleGeneratorCharge_OnPress);
        btnToggleGeneratorChargeBG.Controller.OnHover += new XUiEvent_OnHoverEventHandler(BtnToggleGeneratorCharge_OnHover);
        XUiController childById3 = GetChildById("lblToggleGeneratorCharge");
        if (childById3 != null) lblToggleGeneratorCharge = (XUiV_Label)childById3.ViewComponent;
        XUiController childById4 = GetChildById("sprToggleGeneratorCharge");
        if (childById4 != null) sprToggleGeneratorCharge = (XUiV_Sprite)childById4.ViewComponent;

        btnToggleBatteryChargeHovered = false;
        btnToggleBatteryCharge = GetChildById("btnToggleBatteryCharge");
        btnToggleBatteryChargeBG = (XUiV_Button)btnToggleBatteryCharge.GetChildById("clickable").ViewComponent;
        btnToggleBatteryChargeBG.Controller.OnPress += new XUiEvent_OnPressEventHandler(BtnToggleBatteryCharge_OnPress);
        btnToggleBatteryChargeBG.Controller.OnHover += new XUiEvent_OnHoverEventHandler(BtnToggleBatteryCharge_OnHover);
        XUiController childById5 = GetChildById("lblToggleBatteryCharge");
        if (childById5 != null) lblToggleBatteryCharge = (XUiV_Label)childById5.ViewComponent;
        XUiController childById6 = GetChildById("sprToggleBatteryCharge");
        if (childById6 != null) sprToggleBatteryCharge = (XUiV_Sprite)childById6.ViewComponent;

        turnOff = Localization.Get("xuiTurnOff");
        turnOn = Localization.Get("xuiTurnOn");
    }

    private void BtnToggleSolarCharge_OnHover(XUiController _sender, bool _isOver)
    {
        btnToggleSolarChargeHovered = _isOver;
        RefreshBindings();
    }

    private void BtnToggleGeneratorCharge_OnHover(XUiController _sender, bool _isOver)
    {
        btnToggleGeneratorChargeHovered = _isOver;
        RefreshBindings();
    }

    private void BtnToggleBatteryCharge_OnHover(XUiController _sender, bool _isOver)
    {
        btnToggleBatteryChargeHovered = _isOver;
        RefreshBindings();
    }

    private void BtnToggleSolarCharge_OnPress(XUiController _sender, int _mouseButton)
    {
        Toggle(ref chargeFromSolar, ref lblToggleSolarCharge, ref sprToggleSolarCharge);
        if (Owner == null) return;
        if (Owner.TileEntity != null && Owner.TileEntity.PowerItem != null)
        {
            if (Owner.TileEntity.PowerItem is PowerSource source)
            {
                if (source.ChargeFromSolar != chargeFromSolar)
                {
                    source.ChargeFromSolar = chargeFromSolar;
                    Owner.TileEntity.SetModified();
                }
            }
        }
        else if (Owner.TileEntity != null && Owner.TileEntity.ClientData != null)
        {
            var source = Owner.TileEntity.ClientData;
            if (source.ChargeFromSolar != chargeFromSolar)
            {
                source.ChargeFromSolar = chargeFromSolar;
                Owner.TileEntity.SetModified();
            }
        }
    }

    private void BtnToggleGeneratorCharge_OnPress(XUiController _sender, int _mouseButton)
    {
        Toggle(ref chargeFromGenerator, ref lblToggleGeneratorCharge, ref sprToggleGeneratorCharge);
        if (Owner == null) return;
        if (Owner.TileEntity != null && Owner.TileEntity.PowerItem != null)
        {
            if (Owner.TileEntity.PowerItem is PowerSource source)
            {
                if (source.ChargeFromGenerator != chargeFromGenerator)
                {
                    source.ChargeFromGenerator = chargeFromGenerator;
                    Owner.TileEntity.SetModified();
                }
            }
        }
        else if (Owner.TileEntity != null && Owner.TileEntity.ClientData != null)
        {
            var source = Owner.TileEntity.ClientData;
            if (source.ChargeFromGenerator != chargeFromGenerator)
            {
                source.ChargeFromGenerator = chargeFromGenerator;
                Owner.TileEntity.SetModified();
            }
        }
    }

    private void BtnToggleBatteryCharge_OnPress(XUiController _sender, int _mouseButton)
    {
        Toggle(ref chargeFromBattery, ref lblToggleBatteryCharge, ref sprToggleBatteryCharge);
        if (Owner == null) return;
        if (Owner.TileEntity != null && Owner.TileEntity.PowerItem != null)
        {
            if (Owner.TileEntity.PowerItem is PowerSource source)
            {
                if (source.ChargeFromBattery != chargeFromBattery)
                {
                    source.ChargeFromBattery = chargeFromBattery;
                    Owner.TileEntity.SetModified();
                }
            }
        }
        else if (Owner.TileEntity != null && Owner.TileEntity.ClientData != null)
        {
            var source = Owner.TileEntity.ClientData;
            if (source.ChargeFromBattery != chargeFromBattery)
            {
                source.ChargeFromBattery = chargeFromBattery;
                Owner.TileEntity.SetModified();
            }
        }
    }

    private void Toggle(ref bool flag, ref XUiV_Label label, ref XUiV_Sprite sprite)
    {
        Update(flag = !flag, label, sprite);
    }

    private void Update(bool flag, XUiV_Label label, XUiV_Sprite sprite)
    {
        if (label != null)
        {
            if (flag)
            {
                label.Text = turnOff;
            }
            else
            {
                label.Text = turnOn;
            }
        }
        if (sprite != null)
        {
            if (flag)
            {
                sprite.Color = onColor;
            }
            else
            {
                sprite.Color = offColor;
            }
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();

        if (Owner.TileEntity.PowerItemType != PowerItem.PowerItemTypes.BatteryBank)
        {
            viewComponent.IsVisible = false;
            return;
        }

        viewComponent.IsVisible = true;

        if (Owner != null && Owner.TileEntity != null)
        {
            if (Owner.TileEntity.PowerItem is PowerSource source)
            {
                chargeFromSolar = source.ChargeFromSolar;
                chargeFromGenerator = source.ChargeFromGenerator;
                chargeFromBattery = source.ChargeFromBattery;
            }
            else if (Owner.TileEntity.ClientData != null)
            {
                var data = Owner.TileEntity.ClientData;
                chargeFromSolar = data.ChargeFromSolar;
                chargeFromGenerator = data.ChargeFromGenerator;
                chargeFromBattery = data.ChargeFromBattery;
            }
        }

        Update(chargeFromSolar, lblToggleSolarCharge, sprToggleSolarCharge);
        Update(chargeFromGenerator, lblToggleGeneratorCharge, sprToggleGeneratorCharge);
        Update(chargeFromBattery, lblToggleBatteryCharge, sprToggleBatteryCharge);

    }

}