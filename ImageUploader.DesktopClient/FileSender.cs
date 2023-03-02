﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ImageUploader.DesktopClient.Model;

namespace ImageUploader.DesktopClient
{
    public class FileSender
    {
        private readonly string _baseAddress;

        public FileSender(string baseAddress)
        {
            _baseAddress = baseAddress;
        }

        public async Task<FileModel> GetFileAsync(long id)
        {
            var response = await CreateHttpClient().GetAsync($"api/FileUpload/GetById?id={id}");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error getting the file!");
            }

            var user = await response.Content.ReadAsAsync<FileModel>();
            return user;

        }

        public async Task AddFileAsync(FileModel fileModel)
        {
            var response = await CreateHttpClient().PostAsJsonAsync("api/FileUpload/Create", fileModel);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error when adding file!");
            }
        }

        public async Task DeleteAsync(long id)
        {
            var response = await CreateHttpClient().DeleteAsync($"api/FileUpload/Delete?id={id}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error when deleting file!");
            }
        }

        public async Task UpdateAsync(FileModel fileModel)
        {
            var response = await CreateHttpClient().PostAsJsonAsync($"api/FileUpload/Update", fileModel);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error when updating file!");
            }
        }

        public HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(_baseAddress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}