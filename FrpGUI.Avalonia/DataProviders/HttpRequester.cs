using FrpGUI.Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace FrpGUI.Avalonia.DataProviders
{
    public class HttpRequester(UIConfig config)
    {
        private const string AuthorizationKey = "Authorization";
        private readonly HttpClient httpClient = new HttpClient();
        public string Token { get; private set; }
        protected string BaseApiUrl => config.ServerAddress;

        public void Dispose()
        {
            httpClient.Dispose();
        }

        public async Task<HttpContent> GetAsync(string endpoint)
        {
            WriteAuthorizationHeader();
            var response = await httpClient.GetAsync($"{BaseApiUrl}/{endpoint}");

            if (response.IsSuccessStatusCode)
            {
                return response.Content;
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            await ProcessError(response);
            throw new Exception();
        }

        public Task<T> GetObjectAsync<T>(string endpoint, JsonTypeInfo<T> jsonTypeInfo, params (string Key, string Value)[] query) where T : class
        {
            var querys = query.Select(p => $"{p.Key}={p.Value}");
            return GetObjectAsync<T>(endpoint + "?" + string.Join('&', querys), jsonTypeInfo);
        }

        public async Task<T> GetObjectAsync<T>(string endpoint, JsonTypeInfo<T> jsonTypeInfo)
        {
            var obj = await GetAsync(endpoint);
            if (obj == null)
            {
                //404，抛出错误
                throw new Exception("请求的资源不存在（404 Not Found）");
            }
            using var responseStream = await obj.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(responseStream, jsonTypeInfo);
        }

        public async Task PostAsync<TData>(string endpoint, object data, JsonTypeInfo<TData> jsonTypeInfo)
        {
            WriteAuthorizationHeader();
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);
            var jsonContent = data == null ? null : new StringContent(JsonSerializer.Serialize(data, jsonTypeInfo), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{BaseApiUrl}/{endpoint}", jsonContent);
            await ProcessError(response);
        }

        public async Task PostAsync(string endpoint)
        {
            WriteAuthorizationHeader();
            var response = await httpClient.PostAsync($"{BaseApiUrl}/{endpoint}", null);
            await ProcessError(response);
        }

        public async Task<TResult> PostAsync<TData, TResult>(string endpoint, JsonTypeInfo<TResult> jsonResultTypeInfo, object data, JsonTypeInfo<TData> jsonDataTypeInfo)
        {
            WriteAuthorizationHeader();
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(jsonResultTypeInfo);
            ArgumentNullException.ThrowIfNull(jsonDataTypeInfo);
            var jsonContent = data == null ? null : new StringContent(JsonSerializer.Serialize(data, jsonResultTypeInfo), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{BaseApiUrl}/{endpoint}", jsonContent);

            await ProcessError(response);
            if (response.Content.Headers.ContentLength == 0)
            {
                return default;
            }
            return await JsonSerializer.DeserializeAsync<TResult>(await response.Content.ReadAsStreamAsync(), jsonResultTypeInfo);
        }

        public async Task<TResult> PostAsync<TResult>(string endpoint, JsonTypeInfo<TResult> jsonResultTypeInfo)
        {
            WriteAuthorizationHeader();
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);
            ArgumentNullException.ThrowIfNull(jsonResultTypeInfo);
            var response = await httpClient.PostAsync($"{BaseApiUrl}/{endpoint}", null);

            await ProcessError(response);
            if (response.Content.Headers.ContentLength == 0)
            {
                return default;
            }
            return await JsonSerializer.DeserializeAsync<TResult>(await response.Content.ReadAsStreamAsync(), jsonResultTypeInfo);
        }

        public void WriteAuthorizationHeader()
        {
            if (string.IsNullOrWhiteSpace(config.ServerToken))
            {
                return;
            }
            if (httpClient.DefaultRequestHeaders.TryGetValues(AuthorizationKey, out IEnumerable<string> values))
            {
                var count = values.Count();
                if (count >= 1)
                {
                    if (values.First() == config.ServerToken)
                    {
                        return;
                    }
                    httpClient.DefaultRequestHeaders.Remove(AuthorizationKey);
                    httpClient.DefaultRequestHeaders.Add(AuthorizationKey, config.ServerToken);
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                httpClient.DefaultRequestHeaders.Add(AuthorizationKey, config.ServerToken);
            }
        }
        protected static async Task ProcessError(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response == null)
                {
                    throw new Exception($"API请求失败（{(int)response.StatusCode}{response.StatusCode}）");
                }
                var message = await response.Content.ReadAsStringAsync();
                message = message.Split(new string[] { Environment.NewLine }, StringSplitOptions.None)[0];
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    throw new Exception($"服务器处理错误（500）：{Environment.NewLine}{message}");
                }
                throw new Exception($"API请求失败（{(int)response.StatusCode}{response.StatusCode}）：{Environment.NewLine}{message}");
            }
        }
    }
}