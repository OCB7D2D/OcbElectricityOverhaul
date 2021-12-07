using Audio;
using System;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

public class XUiC_OcbPowerSourceStats : XUiController
{

  public XUiC_OcbPowerSourceWindowGroup	 Owner { get; set; }

  private Color32 onColor = new Color32((byte) 250, byte.MaxValue, (byte) 163, byte.MaxValue);

  private Color32 offColor = (Color32) Color.white;

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

        this.btnToggleSolarChargeHovered = false;
        this.btnToggleSolarCharge = this.GetChildById("btnToggleSolarCharge");
        this.btnToggleSolarChargeBG = (XUiV_Button) this.btnToggleSolarCharge.GetChildById("clickable").ViewComponent;
        this.btnToggleSolarChargeBG.Controller.OnPress += new XUiEvent_OnPressEventHandler(this.btnToggleSolarCharge_OnPress);
        this.btnToggleSolarChargeBG.Controller.OnHover += new XUiEvent_OnHoverEventHandler(this.btnToggleSolarCharge_OnHover);
        XUiController childById1 = this.GetChildById("lblToggleSolarCharge");
        if (childById1 != null) this.lblToggleSolarCharge = (XUiV_Label) childById1.ViewComponent;
        XUiController childById2 = this.GetChildById("sprToggleSolarCharge");
        if (childById2 != null) this.sprToggleSolarCharge = (XUiV_Sprite) childById2.ViewComponent;

        this.btnToggleGeneratorChargeHovered = false;
        this.btnToggleGeneratorCharge = this.GetChildById("btnToggleGeneratorCharge");
        this.btnToggleGeneratorChargeBG = (XUiV_Button) this.btnToggleGeneratorCharge.GetChildById("clickable").ViewComponent;
        this.btnToggleGeneratorChargeBG.Controller.OnPress += new XUiEvent_OnPressEventHandler(this.btnToggleGeneratorCharge_OnPress);
        this.btnToggleGeneratorChargeBG.Controller.OnHover += new XUiEvent_OnHoverEventHandler(this.btnToggleGeneratorCharge_OnHover);
        XUiController childById3 = this.GetChildById("lblToggleGeneratorCharge");
        if (childById3 != null) this.lblToggleGeneratorCharge = (XUiV_Label) childById3.ViewComponent;
        XUiController childById4 = this.GetChildById("sprToggleGeneratorCharge");
        if (childById4 != null) this.sprToggleGeneratorCharge = (XUiV_Sprite) childById4.ViewComponent;

        this.btnToggleBatteryChargeHovered = false;
        this.btnToggleBatteryCharge = this.GetChildById("btnToggleBatteryCharge");
        this.btnToggleBatteryChargeBG = (XUiV_Button) this.btnToggleBatteryCharge.GetChildById("clickable").ViewComponent;
        this.btnToggleBatteryChargeBG.Controller.OnPress += new XUiEvent_OnPressEventHandler(this.btnToggleBatteryCharge_OnPress);
        this.btnToggleBatteryChargeBG.Controller.OnHover += new XUiEvent_OnHoverEventHandler(this.btnToggleBatteryCharge_OnHover);
        XUiController childById5 = this.GetChildById("lblToggleBatteryCharge");
        if (childById5 != null) this.lblToggleBatteryCharge = (XUiV_Label) childById5.ViewComponent;
        XUiController childById6 = this.GetChildById("sprToggleBatteryCharge");
        if (childById6 != null) this.sprToggleBatteryCharge = (XUiV_Sprite) childById6.ViewComponent;

        this.turnOff = Localization.Get("xuiTurnOff");
        this.turnOn = Localization.Get("xuiTurnOn");
    }

    private void btnToggleSolarCharge_OnHover(XUiController _sender, bool _isOver)
    {
        this.btnToggleSolarChargeHovered = _isOver;
        this.RefreshBindings();
    }

    private void btnToggleGeneratorCharge_OnHover(XUiController _sender, bool _isOver)
    {
        this.btnToggleGeneratorChargeHovered = _isOver;
        this.RefreshBindings();
    }

    private void btnToggleBatteryCharge_OnHover(XUiController _sender, bool _isOver)
    {
        this.btnToggleBatteryChargeHovered = _isOver;
        this.RefreshBindings();
    }

    private void btnToggleSolarCharge_OnPress(XUiController _sender, int _mouseButton)
    {
        Toggle(ref chargeFromSolar, ref lblToggleSolarCharge, ref sprToggleSolarCharge);
        if (this.Owner == null) return;
        if (this.Owner.TileEntity != null && this.Owner.TileEntity.PowerItem != null) {
            if (this.Owner.TileEntity.PowerItem is PowerSource source) {
                if (source.ChargeFromSolar != chargeFromSolar) {
                    source.ChargeFromSolar = chargeFromSolar;
                    this.Owner.TileEntity.SetModified();
                }
            }
        }
        else if (this.Owner.TileEntity != null && this.Owner.TileEntity.ClientData != null) {
            var source = this.Owner.TileEntity.ClientData;
            if (source.ChargeFromSolar != chargeFromSolar) {
                source.ChargeFromSolar = chargeFromSolar;
                this.Owner.TileEntity.SetModified();
            }
        }
    }

    private void btnToggleGeneratorCharge_OnPress(XUiController _sender, int _mouseButton)
    {
        Toggle(ref chargeFromGenerator, ref lblToggleGeneratorCharge, ref sprToggleGeneratorCharge);
        if (this.Owner == null) return;
        if (this.Owner.TileEntity != null && this.Owner.TileEntity.PowerItem != null) {
            if (this.Owner.TileEntity.PowerItem is PowerSource source) {
                if (source.ChargeFromGenerator != chargeFromGenerator) {
                    source.ChargeFromGenerator = chargeFromGenerator;
                    this.Owner.TileEntity.SetModified();
                }
            }
        }
        else if (this.Owner.TileEntity != null && this.Owner.TileEntity.ClientData != null) {
            var source = this.Owner.TileEntity.ClientData;
            if (source.ChargeFromGenerator != chargeFromGenerator) {
                source.ChargeFromGenerator = chargeFromGenerator;
                this.Owner.TileEntity.SetModified();
            }
        }
    }

    private void btnToggleBatteryCharge_OnPress(XUiController _sender, int _mouseButton)
    {
        Toggle(ref chargeFromBattery, ref lblToggleBatteryCharge, ref sprToggleBatteryCharge);
        if (this.Owner == null) return;
        if (this.Owner.TileEntity != null && this.Owner.TileEntity.PowerItem != null) {
            if (this.Owner.TileEntity.PowerItem is PowerSource source) {
                if (source.ChargeFromBattery != chargeFromBattery) {
                    source.ChargeFromBattery = chargeFromBattery;
                    this.Owner.TileEntity.SetModified();
                }
            }
        }
        else if (this.Owner.TileEntity != null && this.Owner.TileEntity.ClientData != null) {
            var source = this.Owner.TileEntity.ClientData;
            if (source.ChargeFromBattery != chargeFromBattery) {
                source.ChargeFromBattery = chargeFromBattery;
                this.Owner.TileEntity.SetModified();
            }
        }
    }

    private void Toggle(ref bool flag, ref XUiV_Label label, ref XUiV_Sprite sprite)
    {
        Update(flag = !flag, label, sprite);
    }

    private void Update(bool flag, XUiV_Label label, XUiV_Sprite sprite)
    {
        if (label != null) {
            if (flag) {
                label.Text = this.turnOff;
            }
            else {
                label.Text = this.turnOn;
            }
        }
        if (sprite != null) {
            if (flag) {
                sprite.Color = (Color) this.onColor;
            }
            else {
                sprite.Color = (Color) this.offColor;
            }
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();

        if (this.Owner.TileEntity.PowerItemType != PowerItem.PowerItemTypes.BatteryBank) {
            viewComponent.IsVisible = false;
            return;
        }

        viewComponent.IsVisible = true;

        if (this.Owner != null && this.Owner.TileEntity != null) {
            if (this.Owner.TileEntity.PowerItem is PowerSource source) {
                chargeFromSolar = source.ChargeFromSolar;
                chargeFromGenerator = source.ChargeFromGenerator;
                chargeFromBattery = source.ChargeFromBattery;
            }
            else if (this.Owner.TileEntity.ClientData != null) {
                var data = this.Owner.TileEntity.ClientData;
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