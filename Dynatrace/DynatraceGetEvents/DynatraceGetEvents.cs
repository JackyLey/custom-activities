using Ayehu.Sdk.ActivityCreation.Extension;
using Ayehu.Sdk.ActivityCreation.Interfaces;
using System;
using System.IO;
using System.Net;
using System.Data;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Ayehu.Sdk.ActivityCreation
{
    public class DynatraceGetEvents : IActivity
    {
        #region Private readonly properties

        private readonly string API_REQUEST_URL = "https://{0}.live.dynatrace.com/api/v1/events?eventType={1}";
        private readonly string CONTENT_TYPE = "application/json";
        private readonly string ACCEPT = "application/json";
        private readonly string METHOD = "GET";

        #endregion

        #region Incoming properties 

        public string AuthorizationToken;
        public string EnvironmentId;
        public string EventType;

        #endregion

        #region Public methods

        public ICustomActivityResult Execute()
        {
            var httpWebRequest = HttpRequest();

            HttpWebResponse httpResponse = null;
            try
            {
                httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    throw ex;
                }
                using (var streamReader = new StreamReader(ex.Response.GetResponseStream()))
                {
                    var responseString = streamReader.ReadToEnd();
                    var error_messageerrorSummary = ExposeJson(JObject.Parse(responseString), "message")["error_message"];
                    throw new Exception(error_messageerrorSummary);
                }
            }
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var response = streamReader.ReadToEnd();

                    List<string> eventDataKeys = new List<string>(){ "id",
                                                                    "eventId",
                                                                    "entityId",
                                                                    "correlationId",
                                                                    "startTime",
                                                                    "endTime",
                                                                    "entityName",
                                                                    "impactLevel",
                                                                    "eventType",
                                                                    "eventStatus",
                                                                    "tags",
                                                                    "source",
                                                                    "severityLevel",
                                                                    "annotationDescription"};
                    var _events = JObject.Parse(response)["events"];

                    var eventList = new List<Dictionary<string, string>>();

                    foreach (var _event in _events)
                    {
                        var res = ExposeJson(_event.ToObject<JObject>(), "");
                        if (eventDataKeys.Count == 0)
                        {
                            eventDataKeys.AddRange(res.Keys);
                        }

                        var data = res.ToDictionary(x => x.Key, x => x.Value);
                        eventList.Add(data as Dictionary<string, string>);

                    }
                    foreach (var _event in eventList)
                    {
                        _event["startTime"] = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(long.Parse(_event["startTime"])).ToString();
                        _event["endTime"] = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(long.Parse(_event["endTime"])).ToString();
                    }
                    var dt = GetDataTable(eventList.AsEnumerable(), eventDataKeys);

                    return this.GenerateActivityResult(dt);
                }
            }
            else
            {
                throw new Exception("Error");
            }
        }

        #endregion

        #region Private methods 

        private WebRequest HttpRequest()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format(API_REQUEST_URL, EnvironmentId, string.IsNullOrWhiteSpace(EventType) ? "" : EventType));
            httpWebRequest.ContentType = CONTENT_TYPE;
            httpWebRequest.Accept = ACCEPT;
            httpWebRequest.Headers.Add("Authorization", string.Format("Api-Token {0}", AuthorizationToken));
            httpWebRequest.Method = METHOD;

            return httpWebRequest;
        }

        private IDictionary<string, string> ExposeJson(JObject jObject, string search_till, string append = "")
        {
            var result = new Dictionary<string, string>();

            foreach (var jProperty in jObject.Properties())
            {
                if (append + jProperty.Name == search_till)
                {
                    continue;
                }

                var jToken = jProperty.Value;

                if (jToken.Type == JTokenType.Object)
                {
                    var nested_result = ExposeJson(jToken as JObject, search_till, append + jProperty.Name + "_");

                    if (nested_result == null)
                    {
                        return result;
                    }
                    result = result.Concat(nested_result).ToDictionary(q => q.Key, q => q.Value);
                }
                else if (jToken.Type != JTokenType.Array)
                {
                    result.Add(append + jProperty.Name, jProperty.Value.ToString());
                }
            }

            return result;
        }

        private DataTable GetDataTable(IEnumerable<Dictionary<string, string>> columns, IEnumerable<string> keys)
        {
            DataTable dt = new DataTable("resultSet");

            foreach (var column in keys)
            {
                dt.Columns.Add(column);
            }

            foreach (var item in columns)
            {
                var row = dt.NewRow();

                foreach (var col in item)
                {
                    row[col.Key] = col.Value;
                }

                dt.Rows.Add(row);
            }

            return dt;
        }

        #endregion
    }
}