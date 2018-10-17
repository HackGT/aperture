using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

namespace Aperture
{
    public static class CheckInAPI
    {
        public static readonly string URL = "https://checkin.hack.gt";

        public async static Task<bool> Login(string username, string password)
        {
            var client = new RestClient(URL);
            var request = new RestRequest("/api/user/login", Method.POST);

            request.AddParameter("username", username).AddParameter("password", password);

            var response = await client.ExecuteTaskAsync(request);

            if (!response.IsSuccessful)
            {
                return false;
            }

            string authCookie = null;
            foreach (var cookie in response.Cookies)
            {
                if (cookie.Name == "auth")
                {
                    authCookie = cookie.Value;
                }
            }
            if (string.IsNullOrEmpty(authCookie))
            {
                return false;
            }

            Settings.AuthCookie = authCookie;
            Settings.AuthUsername = username;

            return true;
        }

        public async static Task<string> CheckIn(string uuid, string tag)
        {
            string query = @"
                mutation($user: ID!, $tag: String!) {
                    check_in(user: $user, tag: $tag) {
                        user {
                            name
                        }
                    }
                }";

            var client = new RestClient(URL);
            var request = new RestRequest("/graphql", Method.POST);

            request.AddCookie("auth", Settings.AuthCookie);
            request.AddJsonBody(new {
                query,
                variables = new
                {
                    user = uuid,
                    tag
                }
            });

            var response = await client.ExecuteTaskAsync(request);

            if (!response.IsSuccessful)
            {
                return null;
            }

            var schema = new
            {
                data = new
                {
                    check_in = new
                    {
                        user = new
                        {
                            name = ""
                        }
                    }
                }
            };
            return JsonConvert.DeserializeAnonymousType(response.Content, schema).data?.check_in?.user?.name;
        }

        public async static Task<string> CheckOut(string uuid, string tag)
        {
            string query = @"
                mutation($user: ID!, $tag: String!) {
                    check_out(user: $user, tag: $tag) {
                        user {
                            name
                        }
                    }
                }";

            var client = new RestClient(URL);
            var request = new RestRequest("/graphql", Method.POST);

            request.AddCookie("auth", Settings.AuthCookie);
            request.AddJsonBody(new
            {
                query,
                variables = new
                {
                    user = uuid,
                    tag
                }
            });

            var response = await client.ExecuteTaskAsync(request);

            if (!response.IsSuccessful)
            {
                return null;
            }

            var schema = new
            {
                data = new
                {
                    check_out = new
                    {
                        user = new
                        {
                            name = ""
                        }
                    }
                }
            };
            return JsonConvert.DeserializeAnonymousType(response.Content, schema).data?.check_out?.user?.name;
        }
    }
}
