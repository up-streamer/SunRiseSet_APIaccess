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

            OutputPort light = new OutputPort(Pins.GPIO_PIN_D0, false);
            App sunAPI = new App();

            sunAPI.RequestAddress = "http://api.sunrise-sunset.org/json";
            sunAPI.Latitude = "-22.9523316";
            sunAPI.Longitude ="-43.2202229";
            //sunAPI.GmtOffset = -2;
            //final URL ("http://api.sunrise-sunset.org/json?lat=36.7201600&lng=-4.4203400");

            //sunAPI.Run();
            //{"results":{"sunrise":"6:02:42 AM","sunset":"6:20:35 PM","solar_noon":"12:11:38 PM","day_length":"12:17:53","civil_twilight_begin":"5:36:54 AM","civil_twilight_end":"6:46:22 PM","nautical_twilight_begin":"5:06:36 AM","nautical_twilight_end":"7:16:41 PM","astronomical_twilight_begin":"4:35:47 AM","astronomical_twilight_end":"7:47:30 PM"},"status":"OK"}

            //Test Json parser
            string json = "{\"results\":{\"sunrise\":\"6:00:00 AM\",\"sunset\":\"7:00:00 PM\",\"solar_noon\":\"12:11:38 PM\",\"day_length\":\"12:17:53\",\"civil_twilight_begin\":\"5:36:54 AM\",\"civil_twilight_end\":\"6:46:22 PM\",\"nautical_twilight_begin\":\"5:06:36 AM\",\"nautical_twilight_end\":\"7:16:41 PM\",\"astronomical_twilight_begin\":\"4:35:47 AM\",\"astronomical_twilight_end\":\"7:47:30 PM\"},\"status\":\"OK\"}";

         
          //  var jParsed = (JObject)JsonParser.Parse(results);
           // var innerJson = (JObject)jParsed["results"];

            HandleResults Handle = new HandleResults(json);
            Handle.GmtOffSet = 0;

            if (Handle.getSunRiseMillisec > 0) { 
            }

            if (Handle.getSunSetMillisec > 0) {
            }
            else {
            };
            Debug.Print(Handle.getSunRiseMillisec.ToString());
            Debug.Print(Handle.getSunSetMillisec.ToString());
            Debug.Print(DateTime.Now.ToString());
            int a = Handle.StringToTimeSpan("4:00:00 PM");
            Debug.Print("16:00 timespan in milis from datetime.now " + a.ToString());
            // var jParsed = (JObject)JsonParser.Parse(json);

            //var results = (JObject)jParsed["results"];

            //string sunrise = (string)results["sunrise"];


            //string status = (string)jParsed["status"];

            // Debug.Print(sunset);
            // Debug.Print(sunrise);
            // Debug.Print(status);

            // End of test code


            OutputPort led= new OutputPort(Pins.ONBOARD_LED, false);
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

        private string requestResponse;

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
        /// <summary>
        /// Constructor:
        /// Manipulate Json string API response 
        /// </summary>
        public HandleResults(string JsonString)
        {
            var jParsed = (JObject)JsonParser.Parse(JsonString);
            var innerJson = (JObject)jParsed["results"];

            sunSet = (string)innerJson["sunset"];
            sunRise = (string)innerJson["sunrise"];
            status = (string)jParsed["status"];
        }

        string sunSet;
        string sunRise;
        string status;

        private int gmtoffset;
        public int GmtOffSet
        {
            get { return gmtoffset * 3600000; }
            set { gmtoffset = value; }
        }

        public int getSunRiseMillisec
        {
            get { return (StringToTimeSpan(sunRise) + GmtOffSet); }
        }

        public int getSunSetMillisec
        {
            get { return (StringToTimeSpan(sunSet) + GmtOffSet); }
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
            long inTicks = eventAt.Ticks - DateTime.Now.Ticks;
            return (int)(inTicks / TimeSpan.TicksPerMillisecond);
            
            
        }
    }
}