/////Deployment Email/////

This is a program to run from a CI Server to request your Ops Team
to schedule a deployment of your built packages to your SIT 
environment. The program is designed to run via daily Scheduled
Task, and can be set up to find packages daily, weekly, or any
configurable amount of time. If a problem is encountered during
execution, an email with error information is sent to your admin

To use this, you will need to know:
- An available SMTP Client (You may already have this on your CI Server)
- The absolute build path of your packages (Ant/Maven/Gradle output path)
- The email addresses of who needs to be on your email request

//////////////////////////
