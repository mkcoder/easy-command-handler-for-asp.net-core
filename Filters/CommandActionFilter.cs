using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApplication1.Controllers;

namespace WebApplication1.Filters
{
    public class Aggregate : TypeFilterAttribute
    {
        public Aggregate() : base(typeof(CommandHandler))
        {            
        }
    }
    
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CommandHandlerFor : HttpPostAttribute
    {
        public CommandHandlerFor(string commandName) : base(commandName)
        {
        }
    }

    public sealed class CommandHandler : IAsyncActionFilter, IAsyncResultFilter, IAsyncExceptionFilter
    {
        private readonly ILogger _logger;

        public CommandHandler(ILogger logger)
        {
            _logger = logger;
        }
        
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var isCommandHandler = ((ControllerActionDescriptor) context.ActionDescriptor).MethodInfo.GetCustomAttributes(typeof(CommandHandlerFor));
            if (isCommandHandler.Any())
            {
                Task.Run(() => Console.WriteLine(context));
                ((ICommand)context.ActionArguments.Single().Value).IsValid();
            }
            return Task.Run(() => next());
        }
        
        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var isCommandHandler = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo.GetCustomAttributes(typeof(CommandHandlerFor));
            if (isCommandHandler.Any())
            {
                var result = ((ObjectResult)context.Result).Value;
                if (result is AggregateEvent @e)
                {
                    ((ObjectResult)context.Result).Value = @e;
                }
                else
                {
                    throw new Exception("Commands must return IEnumerable<IEvent> or IEvent");
                }                

            }
            return Task.Run(() => next());
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            return Task.Run(() => Console.WriteLine(context));
        }
    }

    public interface IEvent
    {
    }

    public sealed class AggregateEvent : IEvent
    {
        public Guid EventId { get; private set; }
        public string EventName { get; private set; }
        public int EventVersion { get; private set; }
        public Guid AggregateId { get; private set; }
        public Guid CorrelationId { get; private set; }
        public string CommandName { get; private set; }
        public object Data { get; private set; }

        private AggregateEvent()
        {
            
        }

        public static AggregateEvent CreateEvent<T>(string eventName, Command cmd, T data, int version = 1)
        {
            return new AggregateEvent()
            {
                EventId = Guid.NewGuid(),
                EventName = eventName,
                EventVersion = version,                
                AggregateId = cmd.AggregateId,
                CorrelationId =  cmd.CorrelationId,
                CommandName = cmd.CommandName,
                Data = data
            };
        }
    }


    public interface ICommand
    {
        Guid CommandId { get; set; }
        int CommandVersion { get; set; }
        void IsValid();
    }

    public abstract class Command : ICommand
    {
        public Guid CommandId { get; set; }
        public int CommandVersion { get; set; }
        public Guid CorrelationId { get; set; }
        public string CommandName { get; set; }
        private DateTime _utcCommandDate;
        public DateTime CommandDate
        {
            get => _utcCommandDate;
            set => _utcCommandDate = value.ToUniversalTime();
        }
        public Guid AggregateId { get; set; }

        public virtual void IsValid()
        {
            Assertion.IsNotNull(CommandId);
            Assertion.IsNotDefault(CommandId);
            Assertion.IsNotNull(CorrelationId);
            Assertion.IsNotDefault(CorrelationId);
            Assertion.IsNotNull(CommandName);
            Assertion.IsNotNull(CommandDate);
            Assertion.IsNotDefault(CommandDate);
        }
    }

    public static class Assertion
    {
        public static void IsNotNull(object t, string message = "Object is null")
        {
            if (t == null)
            {
                throw new ArgumentNullException(message);
            }
        }

        public static void IsNotDefault<T>(T t, string message = "Object is null")
        {
            if (t.Equals(default(T)))
            {
                throw new ArgumentNullException(message);
            }
        }
    }
}
