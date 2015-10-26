﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using RabbitMQ.Client;
using RawRabbit.Configuration;
using RawRabbit.Consumer.Contract;
using RawRabbit.Consumer.Eventing;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Operations;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Common
{
	public static class IServiceCollectionExtensions
	{
		public static IServiceCollection AddRawRabbit(this IServiceCollection collection, Action<IServiceCollection> custom)
		{
			collection
				.AddSingleton<RawRabbitConfiguration>(provider => new RawRabbitConfiguration())
				.AddSingleton<IConnectionFactory, ConnectionFactory>(p =>
				{
					var cfg = p.GetService<RawRabbitConfiguration>();
					return new ConnectionFactory
					{
						HostName = cfg.Hostname,
						Password = cfg.Password,
						UserName = cfg.Username,
						AutomaticRecoveryEnabled = true,
						TopologyRecoveryEnabled = true,
					};
				})
				.AddTransient<IMessageSerializer, JsonMessageSerializer>()
				.AddTransient<IConsumerFactory, EventingBasicConsumerFactory>()
				.AddSingleton<IMessageContextProvider<MessageContext>, DefaultMessageContextProvider>(
					p => new DefaultMessageContextProvider(() => Task.FromResult(Guid.NewGuid())))
				.AddSingleton<IChannelFactory, ChannelFactory>() //TODO: Should this be one/application?
				.AddTransient<IConfigurationEvaluator, ConfigurationEvaluator>()
				.AddTransient<INamingConvetions, NamingConvetions>()
				.AddTransient<ISubscriber<MessageContext>, Subscriber<MessageContext>>()
				.AddTransient<IPublisher, Publisher<MessageContext>>()
				.AddTransient<IResponder<MessageContext>, Responder<MessageContext>>()
				.AddTransient<IRequester, Requester<MessageContext>>(
					p => new Requester<MessageContext>(
						p.GetService<IChannelFactory>(),
						p.GetService<IConsumerFactory>(),
						p.GetService<IMessageSerializer>(),
						p.GetService<IMessageContextProvider<MessageContext>>(),
						p.GetService<RawRabbitConfiguration>().RequestTimeout));
			custom?.Invoke(collection);
			return collection;
		}
	}
}