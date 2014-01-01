// This file is part of ModBound.
// 
// ModBound is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ModBound is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ModBound.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModBoundLib.Extensions;
using ModBoundLib.Helpers;
using ModBoundLib.Responses;

namespace ModBoundLib
{
    public class ModBoundApi : IDisposable
    {

        #region Singleton

        private readonly static object _syncRoot = new object();

        private static ModBoundApi _default;

        public static ModBoundApi Default
        {
            get
            {

                lock (_syncRoot)
                {

                    if (_default == null)
                        _default = new ModBoundApi();
                }

                return _default;


            }
        }

        #endregion

        private const string UrlBase = "http://modbound.com/";

        private const string ApiUrlBase = UrlBase + "api/";

        private Cookie _authCookie;

        private string _username;

        private SecureString _password;

        public bool IsSignedIn
        {
            get
            {
                return _authCookie != null && !_authCookie.Expired;
            }
        }

        public ModBoundApi()
        {
        }

        public ModBoundApi(string username, string password)
        {
            SetLoginDetails(username, password);
        }

        public void SetLoginDetails(string username, string password)
        {

            if (_password != null)
                _password.Dispose();

            _username = username;
            _password = password.ToSecureString();

        }

        public bool SignIn()
        {
            return SignInAsync().Result;
        }

        public async Task<bool> SignInAsync()
        {

            _authCookie = await Login();

            return _authCookie != null;

        }

        private async Task<Cookie> Login()
        {

            CookieContainer cookies = new CookieContainer();

            using (HttpClientHandler handler = new HttpClientHandler())
            {

                handler.CookieContainer = cookies;

                using (var httpClient = new HttpClient(handler))
                {

                    var response = await httpClient.PostAsJsonAsync(ApiUrlBase + "Login", new { Username = _username, Password = _password.ToInsecureString() }).ConfigureAwait(false);

                    response.EnsureSuccessStatusCode();

                    bool success = await response.Content.ReadAsAsync<bool>();

                    if (!success)
                        return null;

                    return cookies.GetCookies(new Uri(UrlBase)).Cast<Cookie>().SingleOrDefault(p => p.Name == ".AspNet.ApplicationCookie");


                }

            }

        }

        public static RegisterResponse Register(string username, string password, string email)
        {
            return RegisterAsync(username, password, email).Result;
        }

        public static async Task<RegisterResponse> RegisterAsync(string username, string password, string email)
        {
            using (var httpClient = new HttpClient())
            {

                var response = await httpClient.PostAsJsonAsync(ApiUrlBase + "Register", new { Username = username, Password = password, Email = email }).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                return (await response.Content.ReadAsAsync<RegisterResponse>());

            }

        }

        public async Task<PostModResponse> PostMod(int id, string name, string description, string version, string changes, ModTypes modType)
        {

            await EnsureSignedIn();

            return await Post<PostModResponse>("PostMod", new { ModId = id, Name = name, Description = description, ModType = (int)modType, Changes = changes, Version = version }, _authCookie);

        }

        public async Task<ApiResponse> PostModFiles(int modId, string version, string[] files, bool isScreenshot = false)
        {

            await EnsureSignedIn();

            var dict = new Dictionary<string, string>();

            dict.Add("ModId", modId.ToString());
            dict.Add("Version", version);
            dict.Add("ModFileType", isScreenshot ? "1" : "0");

            var fileInfos = new List<FileInfo>();

            foreach (string file in files)
            {
                if (!string.IsNullOrEmpty(file) && File.Exists(file))
                    fileInfos.Add(new FileInfo(file));
            }

            return await PostFileData<ApiResponse>("PostModFiles", fileInfos.ToArray(), dict, _authCookie);

        }

        public async Task DownloadModFile(int modId, string version, string fileName, string downloadTo)
        {

            await EnsureSignedIn();

            await DownloadFilePost("DownloadModFile", downloadTo, new { ModId = modId, Version = version, FileName = fileName });

        }

        public async Task<ApiResponse> DeleteMod(int modId)
        {

            await EnsureSignedIn();

            return await Post<ApiResponse>("DeleteMod", new { ModId = modId }, _authCookie);
        }

        public async Task<ApiResponse> DeleteModVersion(int modId, string version)
        {

            await EnsureSignedIn();

            return await Post<ApiResponse>("DeleteModVersion", new { ModId = modId, Version = version }, _authCookie);

        }

        public async Task<ApiResponse> DeleteModFile(int modId, string version, string fileName)
        {

            await EnsureSignedIn();

            return await Post<ApiResponse>("DeleteModFile", new { ModId = modId, Version = version, FileName = fileName }, _authCookie);

        }

        public async Task<AccountInfo> GetAccountInfo()
        {

            await EnsureSignedIn();

            return (await Get<AccountInfoResponse>("GetAccountInfo", _authCookie)).AccountInfo;

        }

        public async Task<List<SBMod>> GetMyMods()
        {

            await EnsureSignedIn();

            return (await Get<SBModResponse>("GetMyMods", _authCookie)).Mods;

        }

        public async Task<List<SBMod>> GetMods(int page, string filter = "")
        {

            await EnsureSignedIn();

            return (await Post<SBModResponse>("GetMods", new { Filter = filter, Page = page }, _authCookie)).Mods;

        }

        public async Task<SBMod> GetMod(int id)
        {

            await EnsureSignedIn();

            var response = await Get<SBModResponse>(String.Format("GetMod/{0}", id), _authCookie);

            if (response == null || response.Mods == null || response.Mods.Count == 0)
                return null;

            return response.Mods[0];

        }

        public async Task<SyncedModsResponse> GetSyncedMods()
        {

            await EnsureSignedIn();

            return await Get<SyncedModsResponse>("GetSyncedMods", _authCookie);

        }

        public async Task<ApiResponse> AddSyncedMod(int modId)
        {

            await EnsureSignedIn();

            return await Post<ApiResponse>("AddSyncedMod", new { ModId = modId }, _authCookie);

        }

        public async Task<ApiResponse> RemoveSyncedMod(int modId)
        {

            await EnsureSignedIn();

            return await Post<ApiResponse>("RemoveSyncedMod", new { ModId = modId }, _authCookie);

        }

        private async Task EnsureSignedIn()
        {

            if ((!string.IsNullOrEmpty(_username) && _password != null) && (_authCookie == null || (_authCookie != null && _authCookie.Expired)))
                _authCookie = await Login();

        }

        private static async Task DownloadFileGet(string relUrl, string downloadDir)
        {

            using (var httpClient = new HttpClient())
            {

                var response = await httpClient.GetAsync(ApiUrlBase + relUrl, HttpCompletionOption.ResponseHeadersRead);

                using (var fileStream = File.Create(Path.Combine(downloadDir, response.Content.Headers.ContentDisposition.FileName)))
                {
                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        httpStream.CopyTo(fileStream);
                        fileStream.Flush();
                    }
                }

            }

        }

        private static async Task DownloadFilePost(string relUrl, string downloadTo, object obj)
        {

            using (var httpClient = new HttpClient())
            {

                var response = await httpClient.PostAsJsonAsync(ApiUrlBase + relUrl, obj);

                response.EnsureSuccessStatusCode();

                using (var fileStream = File.Create(downloadTo))
                {
                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        httpStream.CopyTo(fileStream);
                        fileStream.Flush();
                    }
                }

            }

        }

        private static async Task<T> PostFileData<T>(string relUrl, IEnumerable<FileInfo> fInfos, Dictionary<string, string> postData, Cookie authCookie) where T : ApiResponse
        {

            CookieContainer cookies = new CookieContainer();

            if (authCookie != null)
                cookies.Add(authCookie);

            using (HttpClientHandler handler = new HttpClientHandler())
            {

                handler.CookieContainer = cookies;

                using (var client = new HttpClient(handler))
                {

                    using (var content = new MultipartFormDataContent())
                    {

                        foreach (var kvp in postData)
                        {
                            content.Add(new StringContent(kvp.Value), kvp.Key);
                        }

                        foreach (var fInfo in fInfos)
                        {

                            if (!fInfo.Exists)
                                continue;

                            var fileContent = new StreamContent(File.OpenRead(fInfo.FullName));

                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                            {
                                FileName = fInfo.Name
                            };

                            content.Add(fileContent);

                        }

                        var result = await client.PostAsync(ApiUrlBase + relUrl, content).ConfigureAwait(false);

                        result.EnsureSuccessStatusCode();

                        var apiResponse = await result.Content.ReadAsAsync<T>();

                        return apiResponse;

                    }

                }

            }

        }

        private static async Task<T> Post<T>(string relUrl, object obj, Cookie authCookie) where T : ApiResponse
        {

            CookieContainer cookies = new CookieContainer();

            if (authCookie != null)
                cookies.Add(authCookie);

            using (HttpClientHandler handler = new HttpClientHandler())
            {

                handler.CookieContainer = cookies;

                using (var httpClient = new HttpClient(handler))
                {

                    var postResult = await httpClient.PostAsJsonAsync(ApiUrlBase + relUrl, obj).ConfigureAwait(false);

                    postResult.EnsureSuccessStatusCode();

                    return await postResult.Content.ReadAsAsync<T>();

                }

            }

        }

        private static async Task<T> Get<T>(string relUrl, Cookie authCookie) where T : ApiResponse
        {

            CookieContainer cookies = new CookieContainer();

            if (authCookie != null)
                cookies.Add(authCookie);

            using (HttpClientHandler handler = new HttpClientHandler())
            {

                handler.CookieContainer = cookies;

                using (var httpClient = new HttpClient(handler))
                {

                    var getResult = await httpClient.GetAsync(new Uri(ApiUrlBase + relUrl)).ConfigureAwait(false);

                    getResult.EnsureSuccessStatusCode();

                    return await getResult.Content.ReadAsAsync<T>();

                }

            }

        }

        #region Disposal

        public void Dispose()
        {

            Dispose(true);

            GC.SuppressFinalize(this);

        }

        protected virtual void Dispose(bool disposing)
        {

            if (disposing)
            {
                if (_password != null)
                    _password.Dispose();
            }

            _password = null;

        }

        ~ModBoundApi()
        {
            Dispose(false);
        }

        #endregion

    }
}
