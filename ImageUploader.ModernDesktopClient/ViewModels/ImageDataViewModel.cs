﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ImageUploader.ModernDesktopClient.Enums;
using ImageUploader.ModernDesktopClient.Helpers;
using Wpf.Ui.Common.Interfaces;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace ImageUploader.ModernDesktopClient.ViewModels;

public partial class ImageDataViewModel : ObservableObject, INavigationAware
{
    private readonly IFileRestService _fileRestService;
    private readonly IFileService _fileService;
    private readonly MessageBox _messageBox;

    [ObservableProperty] private string? _fileName;
    private bool _isImageChanged;

    [ObservableProperty] private bool _isIndeterminate;
    [ObservableProperty] private bool _isDataLoadIndeterminate;
    private bool _isInitialized;

    [ObservableProperty] private Visibility _isVisible = Visibility.Hidden;
    [ObservableProperty] private Visibility _isDataLoadVisible = Visibility.Hidden;

    [ObservableProperty] private List<FileModel> _loadedData = new();

    [ObservableProperty] private Image _loadedImage = new();

    [ObservableProperty] private ObservableCollection<FileModel> _rowCollection = new();

    [ObservableProperty] private FileModel? _selectedItem;

    public ImageDataViewModel(IFileRestService fileRestService,
        IMessageBoxService messageBoxService,
        IFileService fileService,
        DashboardViewModel dashboardViewModel)
    {
        _fileRestService = fileRestService;
        _fileService = fileService;
        _messageBox = messageBoxService.InitializeMessageBox();
        dashboardViewModel.FileEvent += OnFileEvent;
    }

    public void OnNavigatedTo()
    {
        if (!_isInitialized)
        {
            InitializeDataGrid();
        }
    }

    public void OnNavigatedFrom()
    {
    }

    private void OnFileEvent(TemplateEventArgs<bool>? eventArgs)
    {
        if (eventArgs is { GenericObject: true })
        {
            InitializeDataGrid();
        }
    }

    private async void InitializeDataGrid()
    {
        IsDataLoadVisible = Visibility.Visible;
        IsDataLoadIndeterminate = true;
        var receivedData = await _fileRestService.GetAllDataFromFilesAsync();

        foreach (var fileModel in receivedData)
        {
            RowCollection.Add(fileModel);
        }

        _isInitialized = true;

        IsDataLoadVisible = Visibility.Hidden;
        IsDataLoadIndeterminate = false;
    }

    //BUG after deleting Fix me!
    public async Task DownloadImage()
    {
        try
        {
            if (SelectedItem != null)
            {
                await ExecuteTask(async id =>
                {
                    var files = await _fileRestService.GetFileAsync(id);
                    LoadedImage.Source = ImageConverter.ByteToImage(files.Photo);
                    FileName = SelectedItem.Name;
                }, SelectedItem.Id);
            }
            else
            {
                _messageBox.Show("Error!", "SelectedItem is null.");
            }
        }
        catch (Exception)
        {
            _messageBox.Show("Title", "Could not load image data!");
        }
    }

    //BUG an error occur when the open dialog is close 
    [RelayCommand]
    private void OnFileOpen()
    {
        try
        {
            LoadedImage.Source = _fileService.OpenFileAndGetImageSource();
            _messageBox.Show("Information!", "File has been opened");
            _isImageChanged = true;
        }
        catch (IOException)
        {
            _messageBox.Show("Error!", "Could not open the file!");
        }
    }

    [RelayCommand]
    public async Task DeleteFile()
    {
        if (_messageBox.ButtonLeftName == ButtonName.Ok.ToString())
        {
            if (SelectedItem == null)
            {
                _messageBox.Show("Attention!", "Selected row has incorrect or no data.");
                return;
            }

            if (SelectedItem.Id is 0 or < 0)
            {
                _messageBox.Show("Attention!", "Selected Id is incorrect.");
                return;
            }

            try
            {
                await ExecuteTask(async id =>
                {
                    await _fileRestService.DeleteAsync(id);
                    RowCollection.Clear();
                    InitializeDataGrid();
                }, SelectedItem.Id);
            }
            catch (Exception)
            {
                _messageBox.Show("Error!", "Could not delete the file");
            }
        }
    }

    [RelayCommand]
    public async Task UpdateFile()
    {
        try
        {
            var fileDto = new FileDto
            {
                Id = SelectedItem!.Id,
                Name = FileName,
                LastPhotoName = SelectedItem?.Name,
                DateTime = DateTimeOffset.UtcNow,
                Photo = _fileService.ImageByteArray,
                IsUpdated = _isImageChanged
            };

            await ExecuteTask(async model => await _fileRestService.UpdateAsync(model), fileDto);
        }
        catch (Exception)
        {
            _messageBox.Show("Error!", "Can not update the file");
        }
    }

    private async Task ExecuteTask<T>(Func<T, Task> function, T data)
    {
        IsVisible = Visibility.Visible;
        IsIndeterminate = true;
        await function(data);
        IsIndeterminate = false;
        IsVisible = Visibility.Hidden;
    }
}