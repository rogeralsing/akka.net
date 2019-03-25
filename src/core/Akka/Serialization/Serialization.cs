﻿//-----------------------------------------------------------------------
// <copyright file="Serialization.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Annotations;
using Akka.Util.Internal;
using Akka.Util.Reflection;

namespace Akka.Serialization
{
    /// <summary>
    /// INTERNAL API.
    /// 
    /// Serialization information needed for serializing local actor refs.
    /// </summary>
    [InternalApi]
    public sealed class Information : IEquatable<Information>
    {
        public Information(Address address, ActorSystem system)
        {
            Address = address;
            System = system;
        }

        public Address Address { get; }

        public ActorSystem System { get; }

        public bool Equals(Information other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address.Equals(other.Address) && System.Equals(other.System);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Information)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Address.GetHashCode() * 397) ^ System.GetHashCode();
            }
        }

        public static bool operator ==(Information left, Information right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Information left, Information right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// The serialization system used by Akka.NET to serialize and deserialize objects
    /// per the <see cref="ActorSystem"/>'s serialization configuration.
    /// </summary>
    public class Serialization
    {
        /// <summary>
        /// Needs to be INTERNAL so it can be accessed from tests. Should never be set directly.
        /// </summary>
        [ThreadStatic]
        internal static Information CurrentTransportInfo;

        /// <summary>
        ///  Retrieves the <see cref="Information"/> used for serializing and deserializing
        /// <see cref="IActorRef"/> instances in all serializers.
        /// </summary>
        public static Information CurrentTransportInformation
        {
            get
            {
                if (CurrentTransportInfo == null)
                {
                    throw new InvalidOperationException(
                        "CurrentTransportInformation is not set. Use Serialization.WithTransport<T>.");
                }

                return CurrentTransportInfo;
            }
        }

        private Information SerializationInfo { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="system">TBD</param>
        /// <param name="address">TBD</param>
        /// <param name="action">TBD</param>
        /// <returns>TBD</returns>
        [Obsolete("Obsoloete. Use the SerializeWithTransport<T>(ExtendedActorSystem) method instead.")]
        public static T SerializeWithTransport<T>(ActorSystem system, Address address, Func<T> action)
        {
            CurrentTransportInfo = new Information(address, system);
            var res = action();
            CurrentTransportInfo = null;
            return res;
        }

        /// <summary>
        /// Performs the requested serialization function while also setting
        /// the <see cref="CurrentTransportInformation"/> based on available data
        /// from the <see cref="ActorSystem"/>. Useful when serializing <see cref="IActorRef"/>s.
        /// </summary>
        /// <typeparam name="T">The type of message being serialized.</typeparam>
        /// <param name="system">The <see cref="ActorSystem"/> performing serialization.</param>
        /// <param name="action">The serialization function.</param>
        /// <returns>The serialization output.</returns>
        public static T SerializeWithTransport<T>(ExtendedActorSystem system, Func<T> action)
        {
            var info = system.Provider.SerializationInformation;
            if (CurrentTransportInfo == info)
            {
                CurrentTransportInfo = info;
                return action();
            }

            return action();
        }

        private T SerializeWithTransport<T>(Func<T> action)
        {
            var oldInfo = CurrentTransportInfo;
            try
            {
                if (oldInfo == null)
                    CurrentTransportInfo = SerializationInfo;
                return action();
            }
            finally
            {
                CurrentTransportInfo = oldInfo;
            }
        }

        private readonly Serializer _nullSerializer;

        private readonly ConcurrentDictionary<Type, Serializer> _serializerMap = new ConcurrentDictionary<Type, Serializer>();
        private readonly Dictionary<int, Serializer> _serializersById = new Dictionary<int, Serializer>();
        private readonly Dictionary<string, Serializer> _serializersByName = new Dictionary<string, Serializer>();

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        public Serialization(ExtendedActorSystem system)
        {
            System = system;
            SerializationInfo = system.Provider.SerializationInformation;
            _nullSerializer = new NullSerializer(system);
            AddSerializer("null", _nullSerializer);

            var serializersConfig = system.Settings.Config.GetConfig("akka.actor.serializers").AsEnumerable().ToList();
            var serializerBindingConfig = system.Settings.Config.GetConfig("akka.actor.serialization-bindings").AsEnumerable().ToList();
            var serializerSettingsConfig = system.Settings.Config.GetConfig("akka.actor.serialization-settings");

            foreach (var kvp in serializersConfig)
            {
                var serializerTypeName = kvp.Value.GetString();
                var serializerType = Type.GetType(serializerTypeName);
                if (serializerType == null)
                {
                    system.Log.Warning("The type name for serializer '{0}' did not resolve to an actual Type: '{1}'", kvp.Key, serializerTypeName);
                    continue;
                }

                var serializerConfig = serializerSettingsConfig.GetConfig(kvp.Key);

                var serializer = serializerConfig != null
                    ? (Serializer)Activator.CreateInstance(serializerType, system, serializerConfig)
                    : (Serializer)Activator.CreateInstance(serializerType, system);

                AddSerializer(kvp.Key, serializer);
            }

            foreach (var kvp in serializerBindingConfig)
            {
                var typename = kvp.Key;
                var serializerName = kvp.Value.GetString();
                var messageType = Type.GetType(typename);

                if (messageType == null)
                {

                    system.Log.Warning("The type name for message/serializer binding '{0}' did not resolve to an actual Type: '{1}'", serializerName, typename);
                    continue;
                }


                if (!_serializersByName.TryGetValue(serializerName, out var serializer))
                {
                    system.Log.Warning("Serialization binding to non existing serializer: '{0}'", serializerName);
                    continue;
                }

                AddSerializationMap(messageType, serializer);
            }
        }

        private Serializer GetSerializerByName(string name)
        {
            if (name == null)
                return null;

            _serializersByName.TryGetValue(name, out Serializer serializer);
            return serializer;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public ActorSystem System { get; }

        /// <summary>
        /// Adds the serializer to the internal state of the serialization subsystem
        /// </summary>
        /// <param name="serializer">Serializer instance</param>
        [Obsolete("No longer supported. Use the AddSerializer(name, serializer) overload instead.", true)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSerializer(Serializer serializer)
        {
            _serializersById.Add(serializer.Identifier, serializer);
        }

        /// <summary>
        /// Adds the serializer to the internal state of the serialization subsystem
        /// </summary>
        /// <param name="name">Configuration name of the serializer</param>
        /// <param name="serializer">Serializer instance</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSerializer(string name, Serializer serializer)
        {
            _serializersById.Add(serializer.Identifier, serializer);
            _serializersByName.Add(name, serializer);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="type">TBD</param>
        /// <param name="serializer">TBD</param>
        /// <returns>TBD</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSerializationMap(Type type, Serializer serializer)
        {
            _serializerMap[type] = serializer;
        }

        /// <summary>
        /// Serializes the given message into an array of bytes using whatever serializer is currently configured.
        /// </summary>
        /// <param name="o">The message being serialized.</param>
        /// <returns>A byte array containing the serialized message.</returns>
        public byte[] Serialize(object o)
        {
            return SerializeWithTransport(() => FindSerializerFor(o).ToBinary(o));
        }

        /// <summary>
        /// Deserializes the given array of bytes using the specified serializer id, using the optional type hint to the Serializer.
        /// </summary>
        /// <param name="bytes">TBD</param>
        /// <param name="serializerId">TBD</param>
        /// <param name="type">TBD</param>
        /// <exception cref="SerializationException">
        /// This exception is thrown if the system cannot find the serializer with the given <paramref name="serializerId"/>.
        /// </exception>
        /// <returns>The resulting object</returns>
        public object Deserialize(byte[] bytes, int serializerId, Type type)
        {
            return SerializeWithTransport(() =>
            {
                if (!_serializersById.TryGetValue(serializerId, out var serializer))
                    throw new SerializationException(
                        $"Cannot find serializer with id [{serializerId}] (class [{type?.Name}]). The most probable reason" +
                        " is that the configuration entry 'akka.actor.serializers' is not in sync between the two systems.");

                return serializer.FromBinary(bytes, type);
            });
        }

        /// <summary>
        /// Deserializes the given array of bytes using the specified serializer id, using the optional type hint to the Serializer.
        /// </summary>
        /// <param name="bytes">TBD</param>
        /// <param name="serializerId">TBD</param>
        /// <param name="manifest">TBD</param>
        /// <exception cref="SerializationException">
        /// This exception is thrown if the system cannot find the serializer with the given <paramref name="serializerId"/>
        /// or it couldn't find the given <paramref name="manifest"/> with the given <paramref name="serializerId"/>.
        /// </exception>
        /// <returns>The resulting object</returns>
        public object Deserialize(byte[] bytes, int serializerId, string manifest)
        {
            return SerializeWithTransport(() =>
            {

                if (!_serializersById.TryGetValue(serializerId, out var serializer))
                    throw new SerializationException(
                        $"Cannot find serializer with id [{serializerId}] (manifest [{manifest}]). The most probable reason" +
                        " is that the configuration entry 'akka.actor.serializers' is not in sync between the two systems.");

                if (serializer is SerializerWithStringManifest stringManifest)
                    return stringManifest.FromBinary(bytes, manifest);
                if (string.IsNullOrEmpty(manifest))
                    return serializer.FromBinary(bytes, null);
                Type type;
                try
                {
                    type = TypeCache.GetType(manifest);
                }
                catch (Exception ex)
                {
                    throw new SerializationException(
                        $"Cannot find manifest class [{manifest}] for serializer with id [{serializerId}].", ex);
                }

                return serializer.FromBinary(bytes, type);
            });
        }

        /// <summary>
        /// Returns the Serializer configured for the given object, returns the NullSerializer if it's null.
        /// </summary>
        /// <param name="obj">The object that needs to be serialized</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present</param>
        /// <returns>The serializer configured for the given object type</returns>
        public Serializer FindSerializerFor(object obj, string defaultSerializerName = null)
        {
            return obj == null ? _nullSerializer : FindSerializerForType(obj.GetType(), defaultSerializerName);
        }

        //cache to eliminate lots of typeof operator calls
        private readonly Type _objectType = typeof(object);

        /// <summary>
        /// Returns the configured Serializer for the given Class. The configured Serializer
        /// is used if the configured class `IsAssignableFrom` from the <see cref="Type">type</see>, i.e.
        /// the configured class is a super class or implemented interface. In case of
        /// ambiguity it is primarily using the most specific configured class,
        /// and secondly the entry configured first.
        /// </summary>
        /// <param name="objectType">TBD</param>
        /// <param name="defaultSerializerName">The config name of the serializer to use when no specific binding config is present</param>
        /// <exception cref="SerializationException">
        /// This exception is thrown if the serializer of the given <paramref name="objectType"/> could not be found.
        /// </exception>
        /// <returns>The serializer configured for the given object type</returns>
        public Serializer FindSerializerForType(Type objectType, string defaultSerializerName = null)
        {
            if (_serializerMap.TryGetValue(objectType, out var fullMatchSerializer))
                return fullMatchSerializer;

            Serializer serializer = null;
            Type type = objectType;

            // TODO: see if we can do a better job with proper type sorting here - most specific to least specific (object serializer goes last)
            foreach (var serializerType in _serializerMap)
            {
                // force deferral of the base "object" serializer until all other higher-level types have been evaluated
                if (serializerType.Key.IsAssignableFrom(type) && serializerType.Key != _objectType)
                {
                    serializer = serializerType.Value;
                    break;
                }
            }

            if (serializer == null)
                serializer = GetSerializerByName(defaultSerializerName);

            // do a final check for the "object" serializer
            if (serializer == null)
                _serializerMap.TryGetValue(_objectType, out serializer);

            if (serializer == null)
                throw new SerializationException($"Serializer not found for type {objectType.Name}");

            AddSerializationMap(type, serializer);
            return serializer;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="actorRef">TBD</param>
        /// <returns>TBD</returns>
        public static string SerializedActorPath(IActorRef actorRef)
        {
            if (Equals(actorRef, ActorRefs.NoSender))
                return String.Empty;

            var path = actorRef.Path;
            ExtendedActorSystem originalSystem = null;
            if (actorRef is ActorRefWithCell)
            {
                originalSystem = actorRef.AsInstanceOf<ActorRefWithCell>().Underlying.System.AsInstanceOf<ExtendedActorSystem>();
            }

            if (CurrentTransportInfo == null)
            {
                if (originalSystem == null)
                {
                    var res = path.ToSerializationFormat();
                    return res;
                }
                else
                {
                    try
                    {
                        var defaultAddress = originalSystem.Provider.DefaultAddress;
                        var res = path.ToSerializationFormatWithAddress(defaultAddress);
                        return res;
                    }
                    catch
                    {
                        return path.ToSerializationFormat();
                    }
                }
            }

            //CurrentTransportInformation exists
            var system = CurrentTransportInfo.System;
            var address = CurrentTransportInfo.Address;
            if (originalSystem == null || originalSystem == system)
            {
                var res = path.ToSerializationFormatWithAddress(address);
                return res;
            }
            else
            {
                var provider = originalSystem.Provider;
                var res =
                    path.ToSerializationFormatWithAddress(provider.GetExternalAddressFor(address) ?? provider.DefaultAddress);
                return res;
            }
        }

        internal Serializer GetSerializerById(int serializerId)
        {
            return _serializersById[serializerId];
        }
    }
}
