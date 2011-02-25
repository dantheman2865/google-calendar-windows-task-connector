// Namespace Declaration
using System;
using System.Text;
using System.Xml;

using Google.GData.AccessControl;
using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;

using Microsoft.Win32.TaskScheduler;

// Program start class
class MainProgram
{
    // Main begins program execution.
    static void Main(string[] args)
    {
        String user = "";
        String pass = "";
        String feedUrl = "";
        int daysAhead = 3;

        XmlTextReader fileinput = new XmlTextReader("settings.xml");
        fileinput.Read();
        while (fileinput.EOF == false)
        {
            if (fileinput.NodeType.Equals(XmlNodeType.Element))
            {
                switch (fileinput.LocalName)
                {
                    case "username":
                        user = fileinput.ReadElementContentAsString();
                        break;
                    case "password":
                        pass = fileinput.ReadElementContentAsString();
                        break;
                    case "calendarURI":
                        feedUrl = fileinput.ReadElementContentAsString();
                        break;
                    case "daysAhead":
                        daysAhead = fileinput.ReadElementContentAsInt();
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

        CalendarService myService = new CalendarService("dantheman2865-TaskSchedSync");
        myService.setUserCredentials(user, pass);

        EventQuery myQuery = new EventQuery(feedUrl);
        myQuery.StartTime = DateTime.Now;
        myQuery.EndTime = DateTime.Now.AddDays(daysAhead);
        myQuery.SingleEvents = true;

        EventFeed myResultsFeed = myService.Query(myQuery);

        using (TaskService ts = new TaskService())
        {
            TaskFolder tf = ts.RootFolder;
            try
            {
                tf.CreateFolder("GCal");
            }
            catch
            {
                TaskFolder tmptf = tf.SubFolders["GCal"];
                foreach (Task tmpTask in tmptf.Tasks)
                {
                    tmptf.DeleteTask(tmpTask.Name);
                }
            }
            TaskFolder gtf = tf.SubFolders["GCal"];

            foreach (EventEntry eventEntry in myResultsFeed.Entries)
            {
                //Console.WriteLine(eventEntry.Title.Text + ": " + eventEntry.OriginalEvent.OriginalStartTime.StartTime + "\n" + eventEntry.Content.Content + "\n");
                String[] commands = new String[2];
                commands = eventEntry.Content.Content.Split('|');

                if (commands.Length >= 1)
                {
                    String arguments;
                    String command = commands[0];
                    if (commands.Length < 2) arguments = ""; else arguments = commands[1];
                    Version ver = ts.HighestSupportedVersion;
                    bool newVer = (ver >= new Version(1, 2));

                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = eventEntry.Title.Text;

                    TimeTrigger tTrigger = (TimeTrigger)td.Triggers.Add(new TimeTrigger());
                    tTrigger.StartBoundary = eventEntry.OriginalEvent.OriginalStartTime.StartTime;
                    //tTrigger.EndBoundary = eventEntry.OriginalEvent.OriginalStartTime.EndTime;
                    if (newVer) tTrigger.ExecutionTimeLimit = TimeSpan.FromSeconds(15);
                    if (newVer) tTrigger.Id = "Time test";
                    tTrigger.Repetition.StopAtDurationEnd = true;
                    td.Actions.Add(new ExecAction(command, arguments, null));
                    if (newVer)
                    {
                        td.Settings.WakeToRun = true;
                        td.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;
                        //td.Settings.DeleteExpiredTaskAfter = TimeSpan.FromDays(3);
                    }

                    String filename = eventEntry.Title.Text + " " + eventEntry.OriginalEvent.OriginalStartTime.StartTime.DayOfWeek;
                    gtf.RegisterTaskDefinition(filename, td);
                }
            }
        }
    }
}

