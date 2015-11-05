// Copyright (c) Microsoft. All rights reserved.
// 
// This is an extension of the example that can be found here:
// https://github.com/ms-iot/samples/tree/develop/TempSensor/CS
// tuned by Alessio Moretti, Microsoft Student Partner of the University of Rome Tor Vergata

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Spi;
using Windows.Devices.Gpio;
using Windows.Devices.Enumeration;
using Newtonsoft.Json;
// for debug purposes
using System.Diagnostics;

namespace TemperatureIoT
{
    public sealed partial class MainPage : Page
    {
        /*RaspBerry Pi2  Parameters*/
        private const string SPI_CONTROLLER_NAME = "SPI0";  /* For Raspberry Pi 2, use SPI0                             */
        private const Int32 SPI_CHIP_SELECT_LINE = 0;       /* Line 0 maps to physical pin number 24 on the Rpi2        */

        /*Uncomment if you are using mcp3208/3008 which is 12 bits output */

        // byte[] readBuffer = new byte[3]; /*this is defined to hold the output data*/
        // byte[] writeBuffer = new byte[3] { 0x06, 0x00, 0x00 };//00000110 00; // It is SPI port serial input pin, and is used to load channel configuration data into the device


        /*Uncomment if you are using mcp3002*/
        byte[] readBuffer = new byte[3]; // this is defined to hold the output data
        /* It is SPI port serial input pin, and is used to load channel configuration data into the device*/
        byte[] writeBuffer = new byte[3] { 0x68, 0x00, 0x00 };


        private SpiDevice SpiDisplay;

        // create a timer
        private DispatcherTimer timer;
        private int timer_interval = 5000; //5 seconds interval between each sensor reading
        int res = -1;
        double res_final = -1.0;


        // create an HTTP client
        public String website = "http://iotuniroma2-flask.azurewebsites.net/tempinsert";
        public HttpClient client;

        public MainPage()
        {
            this.InitializeComponent();
            this.InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(timer_interval);
            timer.Tick += Timer_Tick;
            timer.Start();

            InitSPI();
        }

        private async void InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 500000;
                settings.Mode = SpiMode.Mode0; 

                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                SpiDisplay = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);
            }

            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }
        private void Timer_Tick(object sender, object e)
        {
            performReading();
            /* if the reading was correctly perfomed once (to be updated in production releases */
            if (res_final > 0)
            {
                // retrieving the date
                String datetime = DateTime.Now.ToString("hh:mm:ss");
                Debug.WriteLine(datetime + " : " + res_final.ToString());
                postConnection(website, res_final.ToString(), datetime);
            }
        }

        /* creating HTTPClient in order to perform a post connection */
        public async void postConnection(String URI, String temp, String time)
        {
            var postData = new List<KeyValuePair<string, string>>();
            postData.Add(new KeyValuePair<string, string>("temperature", temp));
            postData.Add(new KeyValuePair<string, string>("datetime", time));

            client = new HttpClient();
            try
            {
                HttpResponseMessage response = await client.PostAsync(new Uri(URI, UriKind.Absolute), (HttpContent)new FormUrlEncodedContent(postData));

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                /* uncomment if connection debug 
                Debug.WriteLine("Remote connection success!");
                */
            }
            catch (HttpRequestException e)
            {
                /* uncomment if connection debug 
                Debug.WriteLine(e.Message.ToString() + " - remote connection error :(");
                */
            }
        }

        public void performReading()
        {
            SpiDisplay.TransferFullDuplex(writeBuffer, readBuffer);
            res = convertToInt(readBuffer);
            res_final = res / 2.55;
            temperatureTextBlock.Text = res_final.ToString() + " °C";
        }
        public int convertToInt(byte[] data)
        {
            /*Uncomment if you are using mcp3208/3008 which is 12 bits output */
            /*
             int result = data[1] & 0x0F;
             result <<= 8;
             result += data[2];
             return result;
             */

            /*Uncomment if you are using mcp3002*/
            int result = data[0] & 0x03;
            result <<= 8;
            result += data[1];
            return result;
        }

        
    }
}