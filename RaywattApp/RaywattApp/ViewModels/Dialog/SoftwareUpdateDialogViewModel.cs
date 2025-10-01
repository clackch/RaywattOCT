using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class SoftwareUpdateDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SoftwareUpdateDialogViewModel));

        private readonly UsbDetectionService _usbDetectionService;

        [ObservableProperty]
        private string _statusMessage = "Checking USB device...";

        [ObservableProperty]
        private ObservableCollection<UpdateItem> _updateItems = new ObservableCollection<UpdateItem>();

        [ObservableProperty]
        private string _usbDriveName = "";

        [ObservableProperty]
        private bool _hasUsbFiles = false;

        public SoftwareUpdateDialogViewModel()
        {
            _log.Debug("SoftwareUpdateDialogViewModel");
            _usbDetectionService = new UsbDetectionService();
        }

        public override void SetParameter(object parameter)
        {
            if (parameter is Dictionary<string, object> data)
            {
                Title = data.ContainsKey("title") ? data["title"].ToString() : "Software Update";
            }

            CheckUsbState();
        }

        private void CheckUsbState()
        {
            try
            {
                bool hasUsb = _usbDetectionService.HasUsbDrive();
                
                if (hasUsb)
                {
                    var updateItems = _usbDetectionService.GetUsbUpdateItems();
                    UsbDriveName = _usbDetectionService.GetUsbDriveName();

                    UpdateItems.Clear();
                    foreach (var updateItem in updateItems)
                    {
                        UpdateItems.Add(updateItem);
                    }

                    if (UpdateItems.Any())
                    {
                        StatusMessage = $"Found {UpdateItems.Count} update(s) on USB drive ({UsbDriveName}).";
                        HasUsbFiles = true;
                    }
                    else
                    {
                        StatusMessage = "No update files found on USB drive.";
                        HasUsbFiles = false;
                    }
                }
                else
                {
                    StatusMessage = "USB drive not found. Please connect a USB drive.";
                    HasUsbFiles = false;
                    UpdateItems.Clear();
                }
            }
            catch (System.Exception ex)
            {
                _log.Error($"Error checking USB: {ex.Message}", ex);
                StatusMessage = $"Error checking USB: {ex.Message}";
                HasUsbFiles = false;
                UpdateItems.Clear();
            }
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            _log.Debug("AnswerYes");
            
            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = new Dictionary<string, object>
            {
                { "updateItems", UpdateItems.ToList() },
                { "usbDriveName", UsbDriveName },
                { "shouldUpdate", true }
            };
            
            CloseDialogWithResult(dialog, dialogResults);
        }

        protected override void AnswerNo(IDialogWindow dialog)
        {
            _log.Debug("AnswerNo");
            
            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.No;
            dialogResults.DialogReturn = new Dictionary<string, object>
            {
                { "shouldUpdate", false }
            };
            
            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}