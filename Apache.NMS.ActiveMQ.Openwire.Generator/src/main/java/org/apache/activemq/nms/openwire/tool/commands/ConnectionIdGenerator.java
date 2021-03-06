/**
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
package org.apache.activemq.nms.openwire.tool.commands;

import java.io.PrintWriter;

import org.codehaus.jam.JProperty;

public class ConnectionIdGenerator extends CommandClassGenerator {

    protected void generateConstructors( PrintWriter out ) {
        out.println("        public "+getClassName()+"()");
        out.println("        {");
        out.println("        }");
        out.println("");
        out.println("        public "+getClassName()+"( SessionId sessionId )");
        out.println("        {");
        out.println("            this.value = sessionId.ConnectionId;");
        out.println("        }");
        out.println("");
        out.println("        public "+getClassName()+"( ProducerId producerId )");
        out.println("        {");
        out.println("            this.value = producerId.ConnectionId;");
        out.println("        }");
        out.println("");
        out.println("        public "+getClassName()+"( ConsumerId consumerId )");
        out.println("        {");
        out.println("            this.value = consumerId.ConnectionId;");
        out.println("        }");
        out.println("");

        super.generateConstructors(out);
    }

    protected void generateToStringBody( PrintWriter out ) {
        out.println("            return this.value;");
    }
}
