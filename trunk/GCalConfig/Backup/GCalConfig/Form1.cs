using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Collections;

using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

using Google.GData.AccessControl;
using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        Hashtable calendars = new Hashtable();

        public Form1()
        {
            InitializeComponent();
        }

        private void bConnect_Click(object sender, EventArgs e)
        {
            bConnect.Enabled = false;
            bConnect.Text = "...Wait...";

            calendars.Clear();
            cbCalendars.Items.Clear();

            String user = tbUsername.Text;
            String pass = tbPassword.Text;

            String feedUrl = "https://www.google.com/calendar/feeds/default/owncalendars/full";

            CalendarService myService = new CalendarService("dantheman2865-TaskSchedSync");
            myService.setUserCredentials(user, pass);

            CalendarQuery query = new CalendarQuery();
            query.Uri = new Uri(feedUrl);
            CalendarFeed resultFeed = (CalendarFeed)myService.Query(query);
            //Console.WriteLine("Your calendars:\n");

            //string[,] calendars = new string[resultFeed.Entries.Count, 2];
            foreach (CalendarEntry entry in resultFeed.Entries)
            {
                calendars.Add(entry.Title.Text, entry.Content.AbsoluteUri.ToString());
                cbCalendars.Items.Add(entry.Title.Text);
                //Console.WriteLine(count + ": " + entry.Title.Text + " " + entry.Content.AbsoluteUri + "\n");
            }

            //Console.Write("Select a Calendar: ");
            //int choice = Convert.ToInt32(Console.ReadLine());
            bConnect.Enabled = true;
            bConnect.Text = "Done...";
        }

        private void save_Click(object sender, EventArgs e)
        {
           /* FileStream fsEncrypted = new FileStream("settings.xml",
                FileMode.Create,
                FileAccess.Write);
            DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
            DES.Key = ASCIIEncoding.ASCII.GetBytes(System.Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"));
            DES.IV = ASCIIEncoding.ASCII.GetBytes(System.Environment.GetEnvironmentVariable("PROCESSOR_REVISION"));
            ICryptoTransform desencrypt = DES.CreateEncryptor();
            CryptoStream cryptostream = new CryptoStream(fsEncrypted,
                                desencrypt,
                                CryptoStreamMode.Write);
*/
            XmlTextWriter settingsFile = new XmlTextWriter("settings.xml", null);
            settingsFile.WriteStartDocument();

            settingsFile.WriteStartElement("settings");

            settingsFile.WriteStartElement("username"); settingsFile.WriteString(tbUsername.Text);
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("password"); settingsFile.WriteString(tbPassword.Text);
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("calendarURI"); settingsFile.WriteString(calendars[cbCalendars.Text].ToString());
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("updateFrequency"); settingsFile.WriteString(tbFrequency.Text);
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("daysAhead"); settingsFile.WriteString(tbDaysAhead.Text);
            settingsFile.WriteEndElement();

            settingsFile.WriteEndElement();

            settingsFile.WriteEndDocument();
            settingsFile.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
