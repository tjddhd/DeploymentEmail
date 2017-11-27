using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Globalization;

//A program to find the latest created package from a Continuous Integration server and requests deployment from your Ops Team or lets your admin know if there's a problem
//Designed to run via Daily Scheduled Task
//Assumes you have a readily available SMTP client avaialble and that your server packages build to same readily definable location
namespace DeploymentEmail
{
    class Program
    {
        static void Main(string[] args)
        {
            bool debug = false;

            if (args.Count() > 1)
            {
                System.Net.Mail.MailMessage ErrorMail = new System.Net.Mail.MailMessage();

                ErrorMail.To.Add("admin@example.com");
                ErrorMail.Subject = "Too many arguments in command";
                ErrorMail.From = new System.Net.Mail.MailAddress("admin@example.com");
                ErrorMail.Body = "Too many arguments in command";

                System.Net.Mail.SmtpClient smtp_error = new System.Net.Mail.SmtpClient("your.SMTP.Client.com"); //your SMTP host
                smtp_error.Send(ErrorMail);
            }
            else if (args.Count() == 1)
            {
                if (string.Equals(args[0].ToUpper(), "DEBUG"))
                {
                    debug = true;
                }
                else
                {
                    Console.WriteLine("Wrong argument format. Command takes 1 argument\n\tYou entered: {0}\n\tCorrect Format: DEBUG", args[0]);
                }
            }
            
            var master_directory = new DirectoryInfo("D:\\Master\\Directory\\");
            string release_num = "0";
            try
            {
                release_num = File.ReadAllText(@"d:\release_numNum.txt", Encoding.UTF8);
            }
            catch(Exception e)
            {
                System.Net.Mail.MailMessage ErrorMail = new System.Net.Mail.MailMessage();

                ErrorMail.To.Add("admin@example.com");
                ErrorMail.Subject = "Error Finding Release Number on Server";
                ErrorMail.From = new System.Net.Mail.MailAddress("admin@example.com");
                ErrorMail.Body = "There was a problem getting the release number.\nError message: " + e.Message + 
                                    "\nError Source: " + e.Source + "\nError StackTrace: " + e.StackTrace;

                System.Net.Mail.SmtpClient smtp_error = new System.Net.Mail.SmtpClient("your.SMTP.Client.com"); //your SMTP host
                smtp_error.Send(ErrorMail);
            }

            try
            {
                //Example Packages, these are checked every day
                var pkg1_directory = new DirectoryInfo(master_directory.FullName + "\\Pkg1\\" + release_num);
                var pkg1_packagefile = pkg1_directory.Exists ? pkg1_directory.GetFiles("*" + release_num + "*.zip").OrderByDescending(f => f.LastWriteTime).FirstOrDefault() : new FileInfo("NULL");

                var pkg2_directory = new DirectoryInfo(master_directory.FullName + "\\Pkg2_Release\\" + release_num + "-release");
                var pkg2_packagefile = pkg2_directory.Exists ? pkg2_directory.GetFiles("*" + release_num + "*.zip").OrderByDescending(f => f.LastWriteTime).FirstOrDefault() : new FileInfo("NULL");


                //This example package is requested only every other Thursday, no matter if it changed or not
                var pkg3_directory = new DirectoryInfo(master_directory.FullName + "\\Pkg_3\\Pkg3_" + release_num + ".0");
                var pkg3_packagefile = pkg3_directory.Exists ? pkg3_directory.GetFiles("*" + release_num + "*.zip").OrderByDescending(f => f.LastWriteTime).FirstOrDefault() : new FileInfo("NULL");


                //Configuration that will only request a certain package every other Thursday
                //Code to get week number, get odd numbered weeks since that matches to your end of sprint
                //Will probably stop working eventually depending on leap year
                //Code found here: http://stackoverflow.com/questions/11154673/get-the-correct-week-number-of-a-given-date
                bool thursday_flag = DateTime.Today.DayOfWeek == DayOfWeek.Thursday;
                int week_num = 0;
                DateTime end_of_sprint = DateTime.Today;
                DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(end_of_sprint);
                if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
                {
                    end_of_sprint = end_of_sprint.AddDays(3);
                }
                week_num = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(end_of_sprint, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                bool end_of_sprint_flag = thursday_flag && week_num % 2 == 1;

                //Assumes the time that the Task runs the program is 4pm
                //This sets the past_day to 4pm the previous day
                DateTime past_day = DateTime.Today.AddHours(-8);

                //Daily check times
                bool main_write_today = pkg1_packagefile.Exists ? pkg1_packagefile.CreationTime >= past_day : false;
                bool fs_write_today = pkg2_packagefile.Exists ? pkg2_packagefile.CreationTime >= past_day : false;


                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();

                //Setting up the email specifics based whether you are debugging
                if (!debug)
                {
                    message.To.Add("YourOpsTeamDist@example.com");
                    message.CC.Add("YourDevTeamDist@example.com");
                    message.CC.Add("IndividualStakeHolder@example.com");
                }
                else
                {
                    message.To.Add("admin@example.com");
                    end_of_sprint_flag = true;
                }

                //Setting up the email specifics
                //Uses current day to help keep track of what was requested and when it was requested
                message.Subject = string.Format("Nightly Deployment for {0} from CI Server", DateTime.Today.ToString("D"));
                if (debug) { message.Subject += string.Format(" DEBUG"); }
                message.From = new System.Net.Mail.MailAddress("ServerNoReply@example.com");
                message.IsBodyHtml = true;
                message.Body =
                    "Ops Team,<br/><br/>Can we have a nightly deployment scheduled for 5:30pm tonight to the following SIT systems from <b><font color='red'>CI Server (Hostname)</font></b>:<br/>";

                //Having the package setup like this will allow your email to show if a CI build issue occurred
                //like if the package path was created but your package didn't get zipped to the proper directory
                if (main_write_today || end_of_sprint_flag)
                {
                    message.Body += "<br/><br/><b>Package 1:</b> &#09;&#09;&#09;" + (pkg1_packagefile.Exists ? Path.GetFileNameWithoutExtension(pkg1_packagefile.Name) + "<br/><b>TO:</b>" +
                    "<br/><br/>Store: SIT1<br/>Hostname: 00000000" +
                    "<br/><br/>Store: SIT2<br/>Hostname: 12345678" : "NOT FOUND");
                }

                if (fs_write_today || end_of_sprint_flag)
                {
                    message.Body += "<br/><br/><b>Package 2: </b>&#09;&#09;" + (pkg2_packagefile.Exists ? Path.GetFileNameWithoutExtension(pkg2_packagefile.Name) + "<br/><b>TO:</b>" +
                    "<br/><br/>Store: SIT 1<br/>Hostname  00000000" : "NOT FOUND");
                }

                if (end_of_sprint_flag)
                {
                    //A request set up to be a biweekly Thursday request only
                    message.Body += "<br/><br/><br/>Can we also have a deployment scheduled for 8:00am tomorrow to the entire SIT environment for the following packages:" +
                    "<br/><br/><b>Agents package: </b>&#09;&#09;" + (pkg3_packagefile.Exists ? Path.GetFileNameWithoutExtension(pkg3_packagefile.Name) : "NOT FOUND") +
                    "<br/><br/><br/>These are the latest packages of the current sprint from the Development Team";
                }

                message.Body += "<br/><br/><br/>Thank you!";


                if (main_write_today || fs_write_today || end_of_sprint_flag)
                {
                    System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("your.SMTP.Client.com"); //your SMTP host
                    smtp.Send(message);
                }
            }
            catch(Exception e)
            {
                //Catch-all email for problems with your system
                System.Net.Mail.MailMessage ErrorMail = new System.Net.Mail.MailMessage();

                ErrorMail.To.Add("admin@example.com");
                ErrorMail.Subject = "Error Creating Email on Server";
                ErrorMail.From = new System.Net.Mail.MailAddress("admin@example.com");
                ErrorMail.Body = "There was a problem creating the email.\nError message: " + e.Message +
                                    "\nError Source: " + e.Source + "\nError StackTrace: " + e.StackTrace;

                System.Net.Mail.SmtpClient smtp_error = new System.Net.Mail.SmtpClient("your.SMTP.Client.com"); //your SMTP host
                smtp_error.Send(ErrorMail);
            }
        }
    }
}
