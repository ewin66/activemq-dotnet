/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/*
 *
 *  Marshaler code for OpenWire format for ConnectionError
 *
 *  NOTE!: This file is auto generated - do not modify!
 *         if you need to make a change, please see the Java Classes
 *         in the nms-activemq-openwire-generator module
 *
 */

using System;
using System.Collections;
using System.IO;

using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS.ActiveMQ.OpenWire;
using Apache.NMS.ActiveMQ.OpenWire.V5;

namespace Apache.NMS.ActiveMQ.OpenWire.V5
{
    /// <summary>
    ///  Marshalling code for Open Wire Format for ConnectionError
    /// </summary>
    class ConnectionErrorMarshaller : BaseCommandMarshaller
    {
        /// <summery>
        ///  Creates an instance of the Object that this marshaller handles.
        /// </summery>
        public override DataStructure CreateObject() 
        {
            return new ConnectionError();
        }

        /// <summery>
        ///  Returns the type code for the Object that this Marshaller handles..
        /// </summery>
        public override byte GetDataStructureType() 
        {
            return ConnectionError.ID_CONNECTIONERROR;
        }

        // 
        // Un-marshal an object instance from the data input stream
        // 
        public override void TightUnmarshal(OpenWireFormat wireFormat, Object o, BinaryReader dataIn, BooleanStream bs) 
        {
            base.TightUnmarshal(wireFormat, o, dataIn, bs);

            ConnectionError info = (ConnectionError)o;
            info.Exception = TightUnmarshalBrokerError(wireFormat, dataIn, bs);
            info.ConnectionId = (ConnectionId) TightUnmarshalNestedObject(wireFormat, dataIn, bs);
        }

        //
        // Write the booleans that this object uses to a BooleanStream
        //
        public override int TightMarshal1(OpenWireFormat wireFormat, Object o, BooleanStream bs)
        {
            ConnectionError info = (ConnectionError)o;

            int rc = base.TightMarshal1(wireFormat, o, bs);
            rc += TightMarshalBrokerError1(wireFormat, info.Exception, bs);
        rc += TightMarshalNestedObject1(wireFormat, (DataStructure)info.ConnectionId, bs);

            return rc + 0;
        }

        // 
        // Write a object instance to data output stream
        //
        public override void TightMarshal2(OpenWireFormat wireFormat, Object o, BinaryWriter dataOut, BooleanStream bs)
        {
            base.TightMarshal2(wireFormat, o, dataOut, bs);

            ConnectionError info = (ConnectionError)o;
            TightMarshalBrokerError2(wireFormat, info.Exception, dataOut, bs);
            TightMarshalNestedObject2(wireFormat, (DataStructure)info.ConnectionId, dataOut, bs);
        }

        // 
        // Un-marshal an object instance from the data input stream
        // 
        public override void LooseUnmarshal(OpenWireFormat wireFormat, Object o, BinaryReader dataIn) 
        {
            base.LooseUnmarshal(wireFormat, o, dataIn);

            ConnectionError info = (ConnectionError)o;
            info.Exception = LooseUnmarshalBrokerError(wireFormat, dataIn);
            info.ConnectionId = (ConnectionId) LooseUnmarshalNestedObject(wireFormat, dataIn);
        }

        // 
        // Write a object instance to data output stream
        //
        public override void LooseMarshal(OpenWireFormat wireFormat, Object o, BinaryWriter dataOut)
        {

            ConnectionError info = (ConnectionError)o;

            base.LooseMarshal(wireFormat, o, dataOut);
            LooseMarshalBrokerError(wireFormat, info.Exception, dataOut);
            LooseMarshalNestedObject(wireFormat, (DataStructure)info.ConnectionId, dataOut);
        }
    }
}
