/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Apache.NMS.ActiveMQ.OpenWire;
using Apache.NMS;
using System;


namespace Apache.NMS.ActiveMQ.Commands
{
	public class ActiveMQMapMessage : ActiveMQMessage, IMapMessage
    {
        public const byte ID_ActiveMQMapMessage = 25;
        
        private PrimitiveMap body;
        
        
        public override byte GetDataStructureType()
        {
            return ID_ActiveMQMapMessage;
        }
        
        public IPrimitiveMap Body
        {
            get {
                if (body == null)
                {
                    body = PrimitiveMap.Unmarshal(Content);
                }
                return body;
            }
        }
        
        public override void BeforeMarshall(OpenWireFormat wireFormat)
        {
            if (body == null)
            {
                Content = null;
            }
            else
            {
                Content = body.Marshal();
            }
            
            Tracer.Debug("BeforeMarshalling, content is: " + Content);
			
            base.BeforeMarshall(wireFormat);
        }
        
    }
}

