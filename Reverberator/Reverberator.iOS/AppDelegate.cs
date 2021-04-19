using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BackgroundTasks;
using Foundation;
using UIKit;

namespace Reverberator.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public static string AlertTaskId { get; } = "com.xamarin.Reverberator.alert";
        public static NSString AlertSuccessNotificationName { get; } = new NSString($"{AlertTaskId}.success");

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            BGTaskScheduler.Shared.Register(AlertTaskId, null, task => HandleAppAlert(task as BGAppRefreshTask));

            Application thisapp = new Application();

			if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
			{
				var notificationSettings = UIUserNotificationSettings.GetSettingsForTypes(
					UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound, null
				);

				app.RegisterUserNotificationSettings(notificationSettings);
			}

			if (options != null)
			{
				// check for a local notification
				if (options.ContainsKey(UIApplication.LaunchOptionsLocalNotificationKey))
				{
					var localNotification = options[UIApplication.LaunchOptionsLocalNotificationKey] as UILocalNotification;
					if (localNotification != null)
					{
						UIAlertController okayAlertController = UIAlertController.Create(localNotification.AlertAction, localNotification.AlertBody, UIAlertControllerStyle.Alert);
						okayAlertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));

						Window.RootViewController.PresentViewController(okayAlertController, true, null);

						// reset our badge
						UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
					}
				}
			}

			return base.FinishedLaunching(app, options);
        }

		public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
		{
			// show an alert
			UIAlertController okayAlertController = UIAlertController.Create(notification.AlertAction, notification.AlertBody, UIAlertControllerStyle.Alert);
			okayAlertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));

			UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(okayAlertController, true, null);

			// reset our badge
			UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
		}

		public override void DidEnterBackground(UIApplication application)
		{
			ScheduleAppRefresh();
		}

		#region Scheduling Tasks

		private void ScheduleAppRefresh()
		{
			NSNotificationCenter.DefaultCenter.AddObserver(AlertSuccessNotificationName, RefreshSuccess);

			var request = new BGAppRefreshTaskRequest(AlertTaskId)
			{
				EarliestBeginDate = (NSDate)DateTime.Now.AddMinutes(1) // Fetch no earlier than 15 minutes from now
			};

			BGTaskScheduler.Shared.Submit(request, out NSError error);

			if (error != null)
				Debug.WriteLine($"Could not schedule app refresh: {error}");
		}

		#endregion

		#region Handling Launch for Tasks

		// Fetch the latest feed entries from server.
		void HandleAppAlert(BGAppRefreshTask task)
		{
			ScheduleAppRefresh();

			SendAlert();
		}

		void RefreshSuccess(NSNotification notification)
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(AlertSuccessNotificationName);
			var task = notification.Object as BGAppRefreshTask;
			task?.SetTaskCompleted(true);
		}

		private void SendAlert()
		{
			var notification = new UILocalNotification();

			// set the fire date (the date time in which it will fire)
			notification.FireDate = NSDate.FromTimeIntervalSinceNow(0);

			// configure the alert
			notification.AlertAction = "Alert";
			notification.AlertBody = "Hello from Reverberator";

			// modify the badge
			//notification.ApplicationIconBadgeNumber = 1;

			// set the sound to be the default sound
			notification.SoundName = UILocalNotification.DefaultSoundName;

			UIApplication.SharedApplication.ScheduleLocalNotification(notification);
			NSNotificationCenter.DefaultCenter.RemoveObserver(AlertSuccessNotificationName);
		}

		#endregion
	}
}
