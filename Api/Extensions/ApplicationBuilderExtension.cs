using Microsoft.AspNetCore.Builder;
using Quartz;

namespace Api.Extensions;

public static class ApplicationBuilderExtension
{
    public static IApplicationBuilder ScheduleQuartzJobs(this IApplicationBuilder builder, IScheduler scheduler)
    {
        // // define the job and tie it to our HelloJob class
        // IJobDetail job = JobBuilder.Create<NotificationJob>()
        //     .WithIdentity("notifications", "template") // name "myJob", group "group1"
        //     .Build();
        //
        // // Trigger the job to run now, and then every 40 seconds
        // ITrigger trigger = TriggerBuilder.Create()
        //     .WithIdentity("notifications_trigger", "template")
        //     .StartNow()
        //     .WithSimpleSchedule(x => x
        //         .WithIntervalInSeconds(1)
        //         .RepeatForever())
        //     .Build();
        //
        // // Tell Quartz to schedule the job using our trigger
        // scheduler.ScheduleJob(job, trigger);

        return builder;
    }
}
