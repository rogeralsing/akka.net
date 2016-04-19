﻿//-----------------------------------------------------------------------
// <copyright file="EventFilterFactory_Generated.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Text.RegularExpressions;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Internal;
using Akka.TestKit.Internal.StringMatcher;
namespace Akka.TestKit
{
    public partial class EventFilterFactory
    {
      
        // --- Error ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Create a filter for <see cref="Akka.Event.Error"/> events.
        /// <para><paramref name="message" /> takes priority over <paramref name="start" />.
        /// If <paramref name="message" />!=<c>null</c> the event must match it to be filtered.
        /// If <paramref name="start" />!=<c>null</c> and <paramref name="message" /> has not been specified,
        /// the event must start with the given string to be filtered.
        /// If <paramref name="contains" />!=<c>null</c> and both <paramref name="message" /> and 
				/// <paramref name="start" /> have not been specified,
        /// the event must contain the given string to be filtered.
        /// </para><example>
        /// Error()                                   // filter all Error events
        /// Error("message")                          // filter on exactly matching message
        /// Error(source: obj)                        // filter on event source
        /// Error(start: "Expected")                  // filter on start of message
        /// Error(contains: "Expected")               // filter on part of message
        /// </example>
        /// <remarks>Please note that filtering on the <paramref name="source"/> being
        /// <c>null</c> does NOT work (passing <c>null</c> disables the source filter).
        /// </remarks>
        /// </summary>
        /// <param name="message">Optional. If specified the event must match it exactly to be filtered.</param>
        /// <param name="start">Optional. If specified (and <paramref name="message"/> is not specified), the event must start with the string to be filtered.</param>
        /// <param name="contains">Optional. If specified (and neither <paramref name="message"/> nor <paramref name="start"/> are specified), the event must contain the string to be filtered.</param>
        /// <param name="source">Optional. The event source.</param>
        /// <returns>The new filter</returns>
        public IEventFilterApplier Error(string message = null, string start = null, string contains = null, string source = null)
        {				    
            var messageMatcher = CreateMessageMatcher(message, start, contains);   //This file has been auto generated. Do NOT modify this file directly
            var sourceMatcher = source == null ? null : new EqualsStringAndPathMatcher(source);
            var filter = new ErrorFilter(messageMatcher, sourceMatcher);
            return CreateApplier(filter, _system);
        }

        /// <summary>
        /// Create a filter for <see cref="Akka.Event.Error"/> events. Events must match the specified pattern to be filtered.
        /// <example>
        /// Error(pattern: new Regex("weird.*message"), source: obj) // filter on pattern and source
        /// </example>
        /// <remarks>Please note that filtering on the <paramref name="source"/> being
        /// <c>null</c> does NOT work (passing <c>null</c> disables the source filter).
        /// </remarks>
        /// </summary>
        /// <param name="pattern">The event must match the pattern to be filtered.</param>
        /// <param name="source">>Optional. The event source.</param>
        /// <returns>The new filter</returns>
        public IEventFilterApplier Error(Regex pattern, string source = null)
        {
            var sourceMatcher = source == null ? null : new EqualsStringAndPathMatcher(source);
            var filter = new ErrorFilter(new RegexMatcher(pattern), sourceMatcher);
            return CreateApplier(filter, _system);
        }



        // --- Warning ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Create a filter for <see cref="Akka.Event.Warning"/> events.
        /// <para><paramref name="message" /> takes priority over <paramref name="start" />.
        /// If <paramref name="message" />!=<c>null</c> the event must match it to be filtered.
        /// If <paramref name="start" />!=<c>null</c> and <paramref name="message" /> has not been specified,
        /// the event must start with the given string to be filtered.
        /// If <paramref name="contains" />!=<c>null</c> and both <paramref name="message" /> and 
				/// <paramref name="start" /> have not been specified,
        /// the event must contain the given string to be filtered.
        /// </para><example>
        /// Warning()                                   // filter all Warning events
        /// Warning("message")                          // filter on exactly matching message
        /// Warning(source: obj)                        // filter on event source
        /// Warning(start: "Expected")                  // filter on start of message
        /// Warning(contains: "Expected")               // filter on part of message
        /// </example>
        /// <remarks>Please note that filtering on the <paramref name="source"/> being
        /// <c>null</c> does NOT work (passing <c>null</c> disables the source filter).
        /// </remarks>
        /// </summary>
        /// <param name="message">Optional. If specified the event must match it exactly to be filtered.</param>
        /// <param name="start">Optional. If specified (and <paramref name="message"/> is not specified), the event must start with the string to be filtered.</param>
        /// <param name="contains">Optional. If specified (and neither <paramref name="message"/> nor <paramref name="start"/> are specified), the event must contain the string to be filtered.</param>
        /// <param name="source">Optional. The event source.</param>
        /// <returns>The new filter</returns>
        public IEventFilterApplier Warning(string message = null, string start = null, string contains = null, string source = null)
        {				    
            var messageMatcher = CreateMessageMatcher(message, start, contains);   //This file has been auto generated. Do NOT modify this file directly
            var sourceMatcher = source == null ? null : new EqualsStringAndPathMatcher(source);
            var filter = new WarningFilter(messageMatcher, sourceMatcher);
            return CreateApplier(filter, _system);
        }


        /// <summary>
        /// Create a filter for <see cref="Akka.Event.Warning"/> events. Events must match the specified pattern to be filtered.
        /// <example>
        /// Warning(pattern: new Regex("weird.*message"), source: obj) // filter on pattern and source
        /// </example>
        /// <remarks>Please note that filtering on the <paramref name="source"/> being
        /// <c>null</c> does NOT work (passing <c>null</c> disables the source filter).
        /// </remarks>
        /// </summary>
        /// <param name="pattern">The event must match the pattern to be filtered.</param>
        /// <param name="source">>Optional. The event source.</param>
        /// <returns>The new filter</returns>
        public IEventFilterApplier Warning(Regex pattern, string source = null)
        {
            var sourceMatcher = source == null ? null : new EqualsStringAndPathMatcher(source);
            var filter = new WarningFilter(new RegexMatcher(pattern), sourceMatcher);
            return CreateApplier(filter, _system);
        }



        // --- Info ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Create a filter for <see cref="Akka.Event.Info"/> events.
        /// <para><paramref name="message" /> takes priority over <paramref name="start" />.
        /// If <paramref name="message" />!=<c>null</c> the event must match it to be filtered.
        /// If <paramref name="start" />!=<c>null</c> and <paramref name="message" /> has not been specified,
        /// the event must start with the given string to be filtered.
        /// If <paramref name="contains" />!=<c>null</c> and both <paramref name="message" /> and 
				/// <paramref name="start" /> have not been specified,
        /// the event must contain the given string to be filtered.
        /// </para><example>
        /// Info()                                   // filter all Info events
        /// Info("message")                          // filter on exactly matching message
        /// Info(source: obj)                        // filter on event source
        /// Info(start: "Expected")                  // filter on start of message
        /// Info(contains: "Expected")               // filter on part of message
        /// </example>
        /// <remarks>Please note that filtering on the <paramref name="source"/> being
        /// <c>null</c> does NOT work (passing <c>null</c> disables the source filter).
        /// </remarks>
        /// </summary>
        /// <param name="message">Optional. If specified the event must match it exactly to be filtered.</param>
        /// <param name="start">Optional. If specified (and <paramref name="message"/> is not specified), the event must start with the string to be filtered.</param>
        /// <param name="contains">Optional. If specified (and neither <paramref name="message"/> nor <paramref name="start"/> are specified), the event must contain the string to be filtered.</param>
        /// <param name="source">Optional. The event source.</param>
        /// <returns>The new filter</returns>
        public IEventFilterApplier Info(string message = null, string start = null, string contains = null, string source = null)
        {				    
            var messageMatcher = CreateMessageMatcher(message, start, contains);   //This file has been auto generated. Do NOT modify this file directly
            var sourceMatcher = source == null ? null : new EqualsStringAndPathMatcher(source);
            var filter = new InfoFilter(messageMatcher, sourceMatcher);
            return CreateApplier(filter, _system);
        }


        /// <summary>
        /// Create a filter for <see cref="Akka.Event.Info"/> events. Events must match the specified pattern to be filtered.
        /// <example>
        /// Info(pattern: new Regex("weird.*message"), source: obj) // filter on pattern and source
        /// </example>
        /// <remarks>Please note that filtering on the <paramref name="source"/> being
        /// <c>null</c> does NOT work (passing <c>null</c> disables the source filter).
        /// </remarks>
        /// </summary>
        /// <param name="pattern">The event must match the pattern to be filtered.</param>
        /// <param name="source">>Optional. The event source.</param>
        /// <returns>The new filter</returns>
        public IEventFilterApplier Info(Regex pattern, string source = null)
        {
            var sourceMatcher = source == null ? null : new EqualsStringAndPathMatcher(source);
            var filter = new InfoFilter(new RegexMatcher(pattern), sourceMatcher);
            return CreateApplier(filter, _system);
        }



        // --- Debug ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Create a filter for <see cref="Akka.Event.Debug"/> events.
        /// <para><paramref name="message" /> takes priority over <paramref name="start" />.
        /// If <paramref name="message" />!=<c>null</c> the event must match it to be filtered.
        /// If <paramref name="start" />!=<c>null</c> and <paramref name="message" /> has not been specified,
        /// the event must start with the given string to be filtered.
        /// If <paramref name="contains" />!=<c>null</c> and both <paramref name="message" /> and 
				/// <paramref name="start" /> have not been specified,
        /// the event must contain the given string to be filtered.
        /// </para><example>
        /// Debug()                                   // filter all Debug events
        /// Debug("message")                          // filter on exactly matching message
        /// Debug(source: obj)                        // filter on event source
        /// Debug(start: "Expected")                  // filter on start of message
        /// Debug(contains: "Expected")               // filter on part of message
        /// </example>
        /// <remarks>Please note that filtering on the <paramref name="source"/> being
        /// <c>null</c> does NOT work (passing <c>null</c> disables the source filter).
        /// </remarks>
        /// </summary>
        /// <param name="message">Optional. If specified the event must match it exactly to be filtered.</param>
        /// <param name="start">Optional. If specified (and <paramref name="message"/> is not specified), the event must start with the string to be filtered.</param>
        /// <param name="contains">Optional. If specified (and neither <paramref name="message"/> nor <paramref name="start"/> are specified), the event must contain the string to be filtered.</param>
        /// <param name="source">Optional. The event source.</param>
        /// <returns>The new filter</returns>
        public IEventFilterApplier Debug(string message = null, string start = null, string contains = null, string source = null)
        {				    
            var messageMatcher = CreateMessageMatcher(message, start, contains);   //This file has been auto generated. Do NOT modify this file directly
            var sourceMatcher = source == null ? null : new EqualsStringAndPathMatcher(source);
            var filter = new DebugFilter(messageMatcher, sourceMatcher);
            return CreateApplier(filter, _system);
        }


        /// <summary>
        /// Create a filter for <see cref="Akka.Event.Debug"/> events. Events must match the specified pattern to be filtered.
        /// <example>
        /// Debug(pattern: new Regex("weird.*message"), source: obj) // filter on pattern and source
        /// </example>
        /// <remarks>Please note that filtering on the <paramref name="source"/> being
        /// <c>null</c> does NOT work (passing <c>null</c> disables the source filter).
        /// </remarks>
        /// </summary>
        /// <param name="pattern">The event must match the pattern to be filtered.</param>
        /// <param name="source">>Optional. The event source.</param>
        /// <returns>The new filter</returns>
        public IEventFilterApplier Debug(Regex pattern, string source = null)
        {
            var sourceMatcher = source == null ? null : new EqualsStringAndPathMatcher(source);
            var filter = new DebugFilter(new RegexMatcher(pattern), sourceMatcher);
            return CreateApplier(filter, _system);
        }


    }
}

