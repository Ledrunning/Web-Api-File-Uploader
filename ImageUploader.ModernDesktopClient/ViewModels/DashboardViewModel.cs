﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageUploader.DesktopCommon.Contracts;
using ImageUploader.DesktopCommon.Events;
using ImageUploader.DesktopCommon.Models;
using ImageUploader.ModernDesktopClient.Contracts;
using ImageUploader.ModernDesktopClient.Helpers;
using ImageUploader.ModernDesktopClient.Resources;

namespace ImageUploader.ModernDesktopClient.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IFileRestService _fileRestService;
    private readonly IFileService _fileService;

    [ObservableProperty] private long _fileId;
    [ObservableProperty] private Image _loadedImage = new();

    public DashboardViewModel(IFileRestService fileRestService, IFileService fileService,
        IMessageBoxService messageBoxService) : base(messageBoxService, fileService)
    {
        _fileRestService = fileRestService;
        _fileService = fileService;
    }

    public event TemplateEventHandler<bool>? FileEvent = delegate { };

    //TODO an error occur when the open dialog is close 
    [RelayCommand]
    protected override void OnFileOpen()
    {
        try
        {
            LoadedImage.Source = _fileService.OpenFileAndGetImageSource();
            MessageBoxService.ModernMessageBox.Show(BaseMessages.Information, "File has been opened");
        }
        catch (IOException)
        {
            MessageBoxService.ModernMessageBox.Show(BaseMessages.Error, "Could not load the file!");
        }
    }

    [RelayCommand]
    private void OnImageClear()
    {
        LoadedImage.Source = null;
    }

    [RelayCommand]
    private async Task OnImageUpload()
    {
        try
        {
            if (_fileService.ImageByteArray is { Length: 0 })
            {
                MessageBoxService.ModernMessageBox.Show(BaseMessages.Attention, DashBoardMessages.UploadFileMessage);
            }

            var fileInfo = _fileService.GetFileData(_fileService.GetFilepath());

            var imageDto = new ImageDto
            {
                Name = $"MyPhoto_{DateTime.UtcNow:MMddyyyy_HHmmss}.jpg",
                DateTime = DateTimeOffset.Now,
                CreationTime = fileInfo.creationData,
                FileSize = fileInfo.fileSize,
                Photo = _fileService.ImageByteArray
            };

            await ExecuteTask(async image => { await _fileRestService.AddFileAsync(image); }, imageDto);

            FileEvent?.Invoke(new TemplateEventArgs<bool>(true));

            MessageBoxService.ModernMessageBox.Show(BaseMessages.Information, DashBoardMessages.FileUploadedMessage);
        }
        catch (Exception)
        {
            MessageBoxService.ModernMessageBox.Show(BaseMessages.Error, DashBoardMessages.UploadedFailMessage);
        }
    }

    [RelayCommand]
    private async Task OnImageDownload()
    {
        try
        {
            await ExecuteTask(async id =>
            {
                var imageDto = await _fileRestService.GetFileAsync(id);
                ImageBuffer = imageDto.Photo;
                ImageName = imageDto.Name;
                LoadedImage.Source = ImageConverter.ByteToImage(imageDto.Photo);
            }, FileId);
        }
        catch (Exception)
        {
            MessageBoxService.ModernMessageBox.Show(BaseMessages.Error, DashBoardMessages.FileDownloadFailMessage);
            IsIndeterminate = false;
            IsVisible = Visibility.Hidden;
        }
    }

    [RelayCommand]
    private async Task OnImageDelete()
    {
        if (FileId is 0 or < 0)
        {
            MessageBoxService.ModernMessageBox.Show(BaseMessages.Attention, DashBoardMessages.CorrectIdMessage);
            return;
        }

        try
        {
            await ExecuteTask(async id => { await _fileRestService.DeleteAsync(id); }, FileId);
            FileEvent?.Invoke(new TemplateEventArgs<bool>(true));
            MessageBoxService.ModernMessageBox.Show(BaseMessages.Information, DashBoardMessages.FileDeletedMessage);
            FileId = 0;
        }
        catch (Exception)
        {
            MessageBoxService.ModernMessageBox.Show(BaseMessages.Error, DashBoardMessages.FileDeleteErrorMessage);
        }
    }

    [RelayCommand]
    private void SaveFileToLocalStorage()
    {
        try
        {
            SaveImage();
        }
        catch (Exception)
        {
            MessageBoxService.ModernMessageBox.Show(BaseMessages.Error, DashBoardMessages.FileSaveErrorMessage);
        }
    }
}