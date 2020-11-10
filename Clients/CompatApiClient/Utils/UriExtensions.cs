﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;

namespace CompatApiClient
{
    public static class UriExtensions
    {
        private static readonly Uri FakeHost = new Uri("sc://q"); // s:// will be parsed as file:///s:// for some reason

        public static NameValueCollection ParseQueryString(Uri uri)
        {
            if (!uri.IsAbsoluteUri)
                uri = new Uri(FakeHost, uri);
            return uri.ParseQueryString();
        }

        public static string GetQueryParameter(this Uri uri, string name)
        {
            var parameters = ParseQueryString(uri);
            return parameters[name];
        }

        public static Uri AddQueryParameter(this Uri uri, string name, string value)
        {
            var queryValue = WebUtility.UrlEncode(name) + "=" + WebUtility.UrlEncode(value);
            return AddQueryValue(uri, queryValue);
        }

        public static Uri AddQueryParameters(Uri uri, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var builder = new StringBuilder();
            foreach (var param in parameters)
            {
                if (builder.Length > 0)
                    builder.Append('&');
                builder.Append(Uri.EscapeDataString(param.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(param.Value));
            }
            return AddQueryValue(uri, builder.ToString());
        }

        public static Uri SetQueryParameter(this Uri uri, string name, string value)
        {
            var parameters = ParseQueryString(uri);
            parameters[name] = value;
            return SetQueryValue(uri, FormatUriParams(parameters));
        }

        public static Uri SetQueryParameters(this Uri uri, IEnumerable<KeyValuePair<string, string>> items)
        {
            var parameters = ParseQueryString(uri);
            foreach (var item in items)
                parameters[item.Key] = item.Value;
            return SetQueryValue(uri, FormatUriParams(parameters));
        }

        public static Uri SetQueryParameters(this Uri uri, params (string name, string value)[] items)
        {
            var parameters = ParseQueryString(uri);
            foreach (var (name, value) in items)
                parameters[name] = value;
            return SetQueryValue(uri, FormatUriParams(parameters));
        }

        public static string FormatUriParams(NameValueCollection parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "";

            var result = new StringBuilder();
            foreach (var key in parameters.AllKeys)
            {
                var value = parameters[key];
                if (value == null)
                    continue;

                result.AppendFormat("&{0}={1}", Uri.EscapeDataString(key), Uri.EscapeDataString(value));
            }
            if (result.Length == 0)
                return "";
            return result.ToString(1, result.Length - 1);
        }

        private static Uri AddQueryValue(Uri uri, string queryToAppend)
        {
            var query = uri.IsAbsoluteUri ? uri.Query : new Uri(FakeHost, uri).Query;
            if (!string.IsNullOrEmpty(query) && query.Length > 1)
                query = query[1..] + "&" + queryToAppend;
            else
                query = queryToAppend;
            return SetQueryValue(uri, query);
        }

        private static Uri SetQueryValue(Uri uri, string value)
        {
            var isAbsolute = uri.IsAbsoluteUri;
            if (isAbsolute)
            {
                var builder = new UriBuilder(uri) { Query = value };
                return new Uri(builder.ToString());
            }
            else
            {
                var startWithSlash = uri.OriginalString.StartsWith("/");
                uri = new Uri(FakeHost, uri);
                var builder = new UriBuilder(uri) { Query = value };
                var additionalStrip = startWithSlash ? 0 : 1;
                var newUri = builder.ToString()[(FakeHost.OriginalString.Length + additionalStrip)..];
                return new Uri(newUri, UriKind.Relative);
            }
        }
    }
}