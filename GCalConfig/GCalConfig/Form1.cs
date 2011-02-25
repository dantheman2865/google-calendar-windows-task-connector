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
using System.DirectoryServices.AccountManagement;

using Microsoft.Win32.TaskScheduler;

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

        private void Form_OnLoad(object sender, EventArgs e)
        {
            try
            {
                XmlTextReader fileinput = new XmlTextReader("settings.xml");
                fileinput.Read();
                while (fileinput.EOF == false)
                {
                    if (fileinput.NodeType.Equals(XmlNodeType.Element))
                    {
                        switch (fileinput.LocalName)
                        {
                            case "username":
                                tbUsername.Text = DPAPI.Decrypt(fileinput.ReadElementContentAsString());
                                break;
                            case "password":
                                tbPassword.Text = DPAPI.Decrypt(fileinput.ReadElementContentAsString());
                                break;
                            case "windowsUsername":
                                tbWindowsUser.Text = DPAPI.Decrypt(fileinput.ReadElementContentAsString());
                                break;
                            case "windowsPassword":
                                tbWindowsPassword.Text = DPAPI.Decrypt(fileinput.ReadElementContentAsString());
                                break;
                            case "updateFrequency":
                                tbFrequency.Text = fileinput.ReadElementContentAsInt().ToString();
                                break;
                            case "daysAhead":
                                tbDaysAhead.Text = fileinput.ReadElementContentAsInt().ToString();
                                break;
                            default:
                                fileinput.Read();
                                break;
                        }
                    }
                    else
                    {
                        fileinput.Read();
                    }
                }
                fileinput.Close();
            }
            catch
            { }
        }

        private void bConnect_Click(object sender, EventArgs e)
        {
            bConnect.Enabled = false;
            bConnect.Text = "...Wait...";
            this.Refresh();

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
            save.Enabled = false;
            save.Text = "Saving...";
            this.Refresh();

            XmlTextWriter settingsFile = new XmlTextWriter("settings.xml", null);
            settingsFile.WriteStartDocument();

            settingsFile.WriteStartElement("settings");

            settingsFile.WriteStartElement("username"); settingsFile.WriteString(DPAPI.Encrypt(tbUsername.Text));
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("password"); settingsFile.WriteString(DPAPI.Encrypt(tbPassword.Text));
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("calendarURI"); settingsFile.WriteString(DPAPI.Encrypt(calendars[cbCalendars.Text].ToString()));
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("windowsUsername"); settingsFile.WriteString(DPAPI.Encrypt(tbWindowsUser.Text));
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("windowsPassword"); settingsFile.WriteString(DPAPI.Encrypt(tbWindowsPassword.Text));
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("updateFrequency"); settingsFile.WriteString(tbFrequency.Text);
            settingsFile.WriteEndElement();

            settingsFile.WriteStartElement("daysAhead"); settingsFile.WriteString(tbDaysAhead.Text);
            settingsFile.WriteEndElement();

            settingsFile.WriteEndElement();

            settingsFile.WriteEndDocument();
            settingsFile.Close();

            //Create a task to update the Tasks
            using (TaskService ts = new TaskService())
            {
                int interval = Convert.ToInt16(tbFrequency.Text);

                TaskFolder tf = ts.RootFolder;
                Version ver = ts.HighestSupportedVersion;
                bool newVer = (ver >= new Version(1, 2));

                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Update tasks from the Google Calendar";
                td.Principal.LogonType = TaskLogonType.InteractiveTokenOrPassword;
                td.Principal.UserId = tbWindowsUser.Text;

                TimeTrigger tTrigger = (TimeTrigger)td.Triggers.Add(new TimeTrigger());
                tTrigger.StartBoundary = DateTime.Now;
                tTrigger.EndBoundary = DateTime.Today + TimeSpan.FromDays(1461);
                if (newVer) tTrigger.ExecutionTimeLimit = TimeSpan.FromSeconds(15);
                if (newVer) tTrigger.Id = "Every " + tbFrequency.Text + " minutes";
                tTrigger.Repetition.Duration = TimeSpan.FromDays(1);
                tTrigger.Repetition.Interval = TimeSpan.FromMinutes(interval);
                tTrigger.Repetition.StopAtDurationEnd = true;

                td.Actions.Add(new ExecAction(Application.StartupPath + "\\GCronWindowsConnector.exe"));
                if (newVer)
                {
                    td.Settings.WakeToRun = false;
                    td.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;
                    //td.Settings.DeleteExpiredTaskAfter = TimeSpan.FromDays(3);
                }

                tf.RegisterTaskDefinition("GCalTaskConnector", td, TaskCreation.CreateOrUpdate, tbWindowsUser.Text,tbWindowsPassword.Text);
            }

            save.Enabled = true;
            save.Text = "Success";
        }

        private void tbWindowsUser_MouseClick(object sender, MouseEventArgs e)
        {
            if (tbWindowsUser.Text.Equals("Username") || tbWindowsUser.Text.Equals(""))
            {
                tbWindowsUser.Text = Environment.UserDomainName + "\\" + Environment.UserName;
            }
        }

        private void bTest_Click(object sender, EventArgs e)
        {
            bTest.Enabled = false;
            bTest.Text = "Checking...";
            this.Refresh();

            bool isValid;
            String[] username = new String[2];
            username = tbWindowsUser.Text.Split('\\');

            // create a "principal context" - e.g. your domain (could be machine, too)
            using (PrincipalContext pc = new PrincipalContext(ContextType.Machine, Environment.MachineName))
            {
                // validate the credentials
                isValid = pc.ValidateCredentials(tbWindowsUser.Text, tbWindowsPassword.Text);
            }

            if (isValid)
            {
                bTest.Text = "Works!";
            }
            else
            {
                bTest.Text = "Try Again.";
            }
            bTest.Enabled = true;
        }
    }
}
