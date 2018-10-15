﻿using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.Threading;
using System.Net;
using System.IO;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using MicroJSON;

namespace SunRiseSet
{
    public class Program
    {

        public static void Main()
        {

            Utility.SetLocalTime(new DateTime(2018, 10, 8, 5, 0, 00));
            string timeToUpdate = "1:00:00 PM"; //Scheduled hour to access the API server.
            int gmtOffSet = 0;
            App sunAPI = new App();

            sunAPI.RequestAddress = "http://api.sunrise-sunset.org/json";
            string Latitude = "-22.9523316";
            string Longitude = "-43.2202229";

            //final URL ("http://api.sunrise-sunset.org/json?lat=36.7201600&lng=-4.4203400");

            //sunAPI.Run();

            //{"results":{"sunrise":"6:02:42 AM","sunset":"6:20:35 PM","solar_noon":"12:11:38 PM","day_length":"12:17:53","civil_twilight_begin":"5:36:54 AM","civil_twilight_end":"6:46:22 PM","nautical_twilight_begin":"5:06:36 AM","nautical_twilight_end":"7:16:41 PM","astronomical_twilight_begin":"4:35:47 AM","astronomical_twilight_end":"7:47:30 PM"},"status":"OK"}

            //Test Json parser
            string json = "{\"results\":{\"sunrise\":\"6:00:00 AM\",\"sunset\":\"7:00:00 PM\",\"solar_noon\":\"12:11:38 PM\",\"day_length\":\"12:17:53\",\"civil_twilight_begin\":\"5:36:54 AM\",\"civil_twilight_end\":\"6:46:22 PM\",\"nautical_twilight_begin\":\"5:06:36 AM\",\"nautical_twilight_end\":\"7:16:41 PM\",\"astronomical_twilight_begin\":\"4:35:47 AM\",\"astronomical_twilight_end\":\"7:47:30 PM\"},\"status\":\"OK\"}";


            //  var jParsed = (JObject)JsonParser.Parse(results);
            // var innerJson = (JObject)jParsed["results"];
            Schedule schedule = new Schedule();
            schedule.timeToUpdate = timeToUpdate;
            schedule.gmtOffSet = gmtOffSet;

            schedule.sunAPI.Latitude = Latitude;
            schedule.sunAPI.Longitude = Longitude;

            schedule.run();
            


            #region Old Code
            //while (true)
            //{
            //    HandleResults Handle = new HandleResults(json);
            //    Handle.GmtOffSet = 0;

            //    lightControl.delay = Handle.getSunRiseMillisec;
            //    Thread offDelay = new Thread(lightControl.turnOff);
            //    offDelay.Start();

            //    lightControl.delay = Handle.getSunSetMillisec;
            //    Thread onDelay = new Thread(lightControl.turnOn);
            //    onDelay.Start();

            //    if (Handle.getSunSetMillisec > ONEDAY)
            //    {
            //        lightControl.light.Write(true); //turn light straight on
            //    }
            //    // *** test ***
            //    Debug.Print("getSunRiseMillisec " + Handle.getSunRiseMillisec.ToString());
            //    Debug.Print("getSunSetMillisec " + Handle.getSunSetMillisec.ToString());
            //    Debug.Print("delay to update " + Handle.StringToTimeSpan(delayToUpdate).ToString());
            //    Debug.Print("DateTime.Now " + DateTime.Now.ToString());
            //    int a = Handle.StringToTimeSpan("4:00:00 PM");
            //    Debug.Print("16:00 timespan in millis from datetime.now " + a.ToString());
            //    // *** test ***

            //    Thread.Sleep(Handle.StringToTimeSpan(delayToUpdate));
            //}
            #endregion

            OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);

            while (sunAPI.IsRunning)
            {
                led.Write(true); // turn on the LED
                Thread.Sleep(250); // sleep for 250ms
                led.Write(false); // turn off the LED
                Thread.Sleep(250); // sleep for 250ms

            }

            Debug.Print("App finished.");
        }

    }

    public class Schedule
    {
        public static string jsonString;

        const int ONEDAY = 86400000; // In milliseconds.,
        public string timeToUpdate;
        public int gmtOffSet;
        string defaultSunRiseTime = "6:00:00 AM";
        string defaultSunSetTime = "6:00:00 PM";

        Thread offDelay;
        Thread onDelay;

        public App sunAPI = new App();

        HandleResults Handle = new HandleResults();

        LightControl lightControl = new LightControl();

        public void run()
        {
            Handle.GmtOffSet = gmtOffSet;

            while (true)
            {
                for (int cnt = 1; cnt <= 3; cnt++)
                {
                    sunAPI.Run();

                    if (sunAPI.requestResponse != "error")
                    {
                        jsonString = sunAPI.requestResponse;
                        HandleResults.ParseJson(jsonString);
                        defaultSunRiseTime = HandleResults.sunRiseTime;
                        defaultSunSetTime = HandleResults.sunSetTime;
                        
                        cnt = 3;
                    }
                    else
                    {
                        Thread.Sleep(10000); // wait 10 sec and retry.
                        if (cnt == 3)
                        {
                            HandleResults.sunRiseTime = defaultSunRiseTime;
                            HandleResults.sunSetTime = defaultSunSetTime;
                        }
                    }
                }

                offDelay = null;
                lightControl.delay = Handle.getSunRiseMillisec;
                offDelay = new Thread(lightControl.turnOff);
                offDelay.Start();

                onDelay = null;
                lightControl.delay = Handle.getSunSetMillisec;
                onDelay = new Thread(lightControl.turnOn);
                onDelay.Start();

                if (Handle.getSunSetMillisec > ONEDAY)
                {
                    lightControl.light.Write(true); //turn light straight on
                };
                Thread.Sleep(Handle.StringToTimeSpan(timeToUpdate)); // wait until next schedule
            }
        }

    }

    public class LightControl
    {
        public int delay { get; set; }
        public OutputPort light = new OutputPort(Pins.GPIO_PIN_D0, false);

        public void turnOff()
        {
            Thread.Sleep(delay);
            light.Write(false);
        }

        public void turnOn()
        {
            Thread.Sleep(delay);
            light.Write(true);
        }
    }
    public class App
    {
        NetworkInterface[] _interfaces;

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        private string requestAddress;

        public string RequestAddress
        {
            get { return requestAddress + "?lat=" + Latitude + "&lng=" + Longitude; }
            set { requestAddress = value; }
        }

        public string requestResponse;

        public bool IsRunning { get; set; }

        public void Run()
        {
            this.IsRunning = true;
            bool goodToGo = InitializeNetwork();

            if (goodToGo)
            {
                requestResponse = MakeWebRequest(RequestAddress);
                Debug.Print(requestResponse);
            }
            else
            {
                requestResponse = "error";
            }

            this.IsRunning = false;
        }


        protected bool InitializeNetwork()
        {
            if (Microsoft.SPOT.Hardware.SystemInfo.SystemID.SKU == 3)
            {
                Debug.Print("Wireless tests run only on Device");
                return false;
            }

            Debug.Print("Getting all the network interfaces.");
            _interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // debug output
            ListNetworkInterfaces();

            // loop through each network interface
            foreach (var net in _interfaces)
            {

                // debug out
                ListNetworkInfo(net);

                switch (net.NetworkInterfaceType)
                {
                    case (NetworkInterfaceType.Ethernet):
                        Debug.Print("Found Ethernet Interface");
                        break;
                    case (NetworkInterfaceType.Wireless80211):
                        Debug.Print("Found 802.11 WiFi Interface");
                        break;
                    case (NetworkInterfaceType.Unknown):
                        Debug.Print("Found Unknown Interface");
                        break;
                }

                // check for an IP address, try to get one if it's empty
                return CheckIPAddress(net);
            }

            // if we got here, should be false.
            return false;
        }

        protected string MakeWebRequest(string url)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.Method = "GET";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    Debug.Print("this is what we got from " + url + ": " + result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                return "error";
            }
        }


        protected bool CheckIPAddress(NetworkInterface net)
        {
            int timeout = 10000; // timeout, in milliseconds to wait for an IP. 10,000 = 10 seconds

            // check to see if the IP address is empty (0.0.0.0). IPAddress.Any is 0.0.0.0.
            if (net.IPAddress == IPAddress.Any.ToString())
            {
                Debug.Print("No IP Address");

                if (net.IsDhcpEnabled)
                {
                    Debug.Print("DHCP is enabled, attempting to get an IP Address");

                    // ask for an IP address from DHCP [note this is a static, not sure which network interface it would act on]
                    int sleepInterval = 10;
                    int maxIntervalCount = timeout / sleepInterval;
                    int count = 0;
                    while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any && count < maxIntervalCount)
                    {
                        Debug.Print("Sleep while obtaining an IP");
                        Thread.Sleep(10);
                        count++;
                    };

                    // if we got here, we either timed out or got an address, so let's find out.
                    if (net.IPAddress == IPAddress.Any.ToString())
                    {
                        Debug.Print("Failed to get an IP Address in the alotted time.");
                        return false;
                    }

                    Debug.Print("Got IP Address: " + net.IPAddress.ToString());
                    return true;

                    //NOTE: this does not work, even though it's on the actual network device. [shrug]
                    // try to renew the DHCP lease and get a new IP Address
                    //net.RenewDhcpLease ();
                    //while (net.IPAddress == "0.0.0.0") {
                    //	Thread.Sleep (10);
                    //}

                }
                else
                {
                    Debug.Print("DHCP is not enabled, and no IP address is configured, bailing out.");
                    return false;
                }
            }
            else
            {
                Debug.Print("Already had IP Address: " + net.IPAddress.ToString());
                return true;
            }

        }

        protected void ListNetworkInterfaces()
        {
            foreach (var net in _interfaces)
            {
                switch (net.NetworkInterfaceType)
                {
                    case (NetworkInterfaceType.Ethernet):
                        Debug.Print("Found Ethernet Interface");
                        break;
                    case (NetworkInterfaceType.Wireless80211):
                        Debug.Print("Found 802.11 WiFi Interface");
                        break;
                    case (NetworkInterfaceType.Unknown):
                        Debug.Print("Found Unknown Interface");
                        break;
                }
            }
        }

        protected void ListNetworkInfo(NetworkInterface net)
        {
            Debug.Print("MAC Address: " + BytesToHexString(net.PhysicalAddress));
            Debug.Print("DHCP enabled: " + net.IsDhcpEnabled.ToString());
            Debug.Print("Dynamic DNS enabled: " + net.IsDynamicDnsEnabled.ToString());
            Debug.Print("IP Address: " + net.IPAddress.ToString());
            Debug.Print("Subnet Mask: " + net.SubnetMask.ToString());
            Debug.Print("Gateway: " + net.GatewayAddress.ToString());

            if (net is Wireless80211)
            {
                var wifi = net as Wireless80211;
                Debug.Print("SSID:" + wifi.Ssid.ToString());
            }

        }

        private static string BytesToHexString(byte[] bytes)
        {
            string hexString = string.Empty;

            // Create a character array for hexidecimal conversion.
            const string hexChars = "0123456789ABCDEF";

            // Loop through the bytes.
            for (byte b = 0; b < bytes.Length; b++)
            {
                if (b > 0)
                    hexString += "-";

                // Grab the top 4 bits and append the hex equivalent to the return string.        
                hexString += hexChars[bytes[b] >> 4];

                // Mask off the upper 4 bits to get the rest of it.
                hexString += hexChars[bytes[b] & 0x0F];
            }

            return hexString;
        }

    }

    public class HandleResults
    {
        #region Old Code
        //public HandleResults(string JsonString)
        //{ 
        //    var jParsed = (JObject)JsonParser.Parse(JsonString);
        //    var innerJson = (JObject)jParsed["results"];

        //    sunSetTime = (string)innerJson["sunset"];
        //    sunRiseTime = (string)innerJson["sunrise"];
        //    status = (string)jParsed["status"];
        //}
        #endregion

        public static string sunRiseTime;
        public static string sunSetTime;
        public static string status;

        /// <summary>
        /// Manipulate Json string from API response 
        /// </summary>
        public static void ParseJson(string JsonString)
        {

            var jParsed = (JObject)JsonParser.Parse(JsonString);
            var innerJson = (JObject)jParsed["results"];

            sunSetTime = (string)innerJson["sunset"];
            sunRiseTime = (string)innerJson["sunrise"];
            status = (string)jParsed["status"];
        }

        private int gmtoffset;
        public int GmtOffSet
        {
            get { return gmtoffset * 3600000; } // 1 hour in milliseconds
            set { gmtoffset = value; }
        }

        public int getSunRiseMillisec
        {
            get { return (StringToTimeSpan(sunRiseTime) + GmtOffSet); }
        }

        public int getSunSetMillisec
        {
            get { return (StringToTimeSpan(sunSetTime) + GmtOffSet); }
        }


        //Debug.Print(DateTime.Now.ToString());

        /// <summary>
        /// Get time as a string "hh:mm:ss AM/PM" and
        /// return today timeSpan in milliseconds from DateTime.Now
        /// </summary>
        public int StringToTimeSpan(string timeString)
        {
            char[] charSeparator = new char[] { ' ' };
            string[] ts = timeString.Split(charSeparator);
            timeString = ts[0];
            string meridiem = ts[1].ToUpper();
            Debug.Print("--->" + timeString);

            charSeparator = new char[] { ':' };
            timeString = timeString.Substring(0, timeString.Length);
            string[] timeStringArray = timeString.Split(charSeparator);

            int hours = int.Parse(timeStringArray[0]);
            if (meridiem == "PM") { hours = hours + 12; }
            int minutes = int.Parse(timeStringArray[1]);
            int seconds = int.Parse(timeStringArray[2]);
            Debug.Print(hours.ToString() + ":" + minutes.ToString() + ":" + seconds.ToString());

            Debug.Print("Today: " + DateTime.Today.ToString());
            DateTime eventAt = DateTime.Today + new TimeSpan(hours, minutes, seconds);
            long inTicks = eventAt.Ticks - DateTime.Now.Ticks; // Using ticks to reduce calc error.
            if (inTicks <= 0)
            {
                eventAt = DateTime.Today.AddDays(1) + new TimeSpan(hours, minutes, seconds); //Next day.
                inTicks = eventAt.Ticks - DateTime.Now.Ticks;
            };
            return (int)(inTicks / TimeSpan.TicksPerMillisecond);


        }
    }
}