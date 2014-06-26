using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RM024
{
    class Program
    {
        enum EEPROM : byte
        {
            ProductID = 0x00,
            Mode = 0x41,
            BaudRate = 0x42,
            Control0 = 0x45,
            RFProfile = 0x54,
            Control1 = 0x56,
            Control2 = 0x57,
            Destination_MAC = 0x70,
            SystemID = 0x76,
            MAC = 0x80,

            API = 0xC1
        }

        enum EEPROMLength : byte
        {
            ProductID = 0x23,
            Mode = 0x01,
            BaudRate = 0x01,
            RFProfile = 0x01,
            Control0 = 0x01,
            Control1 = 0x01,
            Control2 = 0x01,
            Destination_MAC = 0x06,
            SystemID = 0x01,
            MAC = 0x06,
            API = 0x01
        }

        enum Baudrate : byte
        {
            b_230400= 0x0A,
            b_11520	= 0x09,
            b_57600	= 0x08,
            b_38400	= 0x07,
            b_28000	= 0x06,
            b_19200	= 0x05,
            b_14400	= 0x04,
            b_9600	= 0x03,
            b_4800	= 0x02,
            b_2400	= 0x01,
            b_1200	= 0x00
        }
        static SerialPort port;
        static void Main(string[] args)
        {
            try
            {
                port = new SerialPort("COM19", 57600, Parity.None, 8, StopBits.One);
                port.DataReceived += port_DataReceived;
                port.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return;
            }

            /**
             * CONFIG STUFF HERE 
             */
            bool isServer = false;
            /**
             * CONFIG STUFF STOP
             */ 

            System.Threading.Thread.Sleep(500);
            enterCommandMode();
            //configureRadio(true);
            System.Threading.Thread.Sleep(500);
            eepromRead(EEPROM.MAC, EEPROMLength.MAC);

            eepromRead(EEPROM.Destination_MAC, EEPROMLength.Destination_MAC);
            if (isServer)
            {
                eepromWrite(EEPROM.Destination_MAC, EEPROMLength.Destination_MAC, new byte[] { 0x00, 0x50, 0x67, 0xA3, 0x60, 0x8f });
            }
            else
            {
                eepromWrite(EEPROM.Destination_MAC, EEPROMLength.Destination_MAC, new byte[] { 0x00, 0x50, 0x67, 0xA3, 0x60, 0xA0 });
            }
            eepromRead(EEPROM.Destination_MAC, EEPROMLength.Destination_MAC);

            eepromRead(EEPROM.Mode, EEPROMLength.Mode);
            eepromRead(EEPROM.SystemID, EEPROMLength.SystemID);
            eepromRead(EEPROM.RFProfile, EEPROMLength.RFProfile);
            eepromRead(EEPROM.Control0, EEPROMLength.Control0);

            eepromRead(EEPROM.Control1, EEPROMLength.Control1);
            if (isServer)
            {
                eepromWrite(EEPROM.Control1, EEPROMLength.Control1, new byte[] { 0x61 });
            }
            else
            {
                eepromWrite(EEPROM.Control1, EEPROMLength.Control1, new byte[] { 0xF9 });
            }
            eepromRead(EEPROM.Control1, EEPROMLength.Control1);
            
            eepromRead(EEPROM.Control2, EEPROMLength.Control2);
            eepromRead(EEPROM.API, EEPROMLength.API);

            eepromRead(EEPROM.BaudRate, EEPROMLength.BaudRate);
            eepromWrite(EEPROM.BaudRate, EEPROMLength.BaudRate, new byte[] { (byte)Baudrate.b_57600 });
            eepromRead(EEPROM.BaudRate, EEPROMLength.BaudRate);
            exitCommandMode();

            
            Console.Read();
            port.Close();
        }

        static void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sport = (SerialPort)sender;

            int b;
            for (int i = sport.BytesToRead; i > 0; --i)
            {
                b = sport.ReadByte();
                if (b == 0xCC)
                {
                    Console.Write("\n");
                }
                Console.Write("{0:x2} ", b);
            }
        }

        static void configureRadio(bool isServer)
        {
            System.Threading.Thread.Sleep(500);
            byte[] data = new byte[10];
            data[0] = 0x00;
            eepromWrite(EEPROM.RFProfile, EEPROMLength.RFProfile, data);

            System.Threading.Thread.Sleep(500);
            data[0] = (isServer) ? (byte)0x01 : (byte)0x02;
            eepromWrite(EEPROM.Mode, EEPROMLength.Mode, data);

        }

        static void enterCommandMode()
        {
            Console.WriteLine("\nEnter Command Mode");
            port.Write("AT+++\n");
        }

        static void exitCommandMode()
        {
            Console.WriteLine("\nExit Command Mode");
            byte[] buf = new byte[5];
            buf[0] = 0xCC;
            buf[1] = 0x41;
            buf[2] = 0x54;
            buf[3] = 0x4f;
            buf[4] = 0x0D;
            port.Write(buf, 0, 5);
        }

        static void getStatus()
        {
            byte[] buf = new byte[3];
            buf[0] = 0xCC;
            buf[1] = 0x00;
            buf[2] = 0x00;
            port.Write(buf, 0, 3);
        }

        static void eepromRead(EEPROM address, EEPROMLength length)
        {
            Console.Write("\nGET " + Enum.GetName(typeof(EEPROM), address));
            byte[] buf = new byte[4];
            buf[0] = 0xCC;
            buf[1] = 0xC0;
            buf[2] = (byte)address;
            buf[3] = (byte)length;
            port.Write(buf, 0, 4);
            System.Threading.Thread.Sleep(250);
        }

        static void eepromWrite(EEPROM address, EEPROMLength length, byte[] data)
        {
            Console.Write("\nSET " + Enum.GetName(typeof(EEPROM), address) + ' ');
            byte[] buf = new byte[4];
            buf[0] = 0xCC;
            buf[1] = 0xC1;
            buf[2] = (byte)address;
            buf[3] = (byte)length;

            port.Write(buf, 0, 4);
            port.Write(data, 0, (int)length);
            System.Threading.Thread.Sleep(250);
        }
    }
}
