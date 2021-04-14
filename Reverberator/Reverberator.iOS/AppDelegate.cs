using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            return base.FinishedLaunching(app, options);
        }

		public override void DidEnterBackground(UIApplication application)
		{
			ScheduleAppRefresh();
		}

		#region Scheduling Tasks

		void ScheduleAppRefresh()
		{
			NSNotificationCenter.DefaultCenter.AddObserver(AlertSuccessNotificationName, RefreshSuccess);

			var request = new BGAppRefreshTaskRequest(AlertTaskId)
			{
				EarliestBeginDate = (NSDate)DateTime.Now.AddMinutes(15) // Fetch no earlier than 15 minutes from now
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

			task.ExpirationHandler = () => operations.CancelOperations();

			operations.FetchLatestPosts(task);
		}

		void RefreshSuccess(NSNotification notification)
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(AlertSuccessNotificationName);
			var task = notification.Object as BGAppRefreshTask;
			task?.SetTaskCompleted(true);
		}

		#endregion
	}
}
