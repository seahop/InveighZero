﻿/*
 * BSD 3-Clause License
 *
 * Copyright (c) 2021, Kevin Robertson
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, this
 * list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 *
 * 3. Neither the name of the copyright holder nor the names of its
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission. 
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.IO;
using Quiddity.NTLM;

namespace Quiddity.SMB2
{
    class SMB2SessionSetupResponse
    {
        // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/0324190f-a31b-4666-9fa9-5c624273a694
        public ushort StructureSize { get; set; }
        public ushort SessionFlags { get; set; }
        public ushort SecurityBufferOffset { get; set; }
        public ushort SecurityBufferLength { get; set; }
        public byte[] Buffer { get; set; }

        internal SMB2SessionSetupResponse()
        {
            this.StructureSize = 9;
            this.SessionFlags = 0;
            this.SecurityBufferOffset = 72;
            this.SecurityBufferLength = 0;
            this.Buffer = new byte[0];
        }

        internal byte[] GetBytes()
        {
            this.SecurityBufferLength = (ushort)Buffer.Length;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                PacketWriter packetWriter = new PacketWriter(memoryStream);
                packetWriter.Write(this.StructureSize);
                packetWriter.Write(this.SessionFlags);
                packetWriter.Write(this.SecurityBufferOffset);
                packetWriter.Write(this.SecurityBufferLength);

                if (this.SecurityBufferLength > 0)
                {
                    packetWriter.Write(this.Buffer);
                }

                return memoryStream.ToArray();
            }

        }

        internal void Pack(out byte[] ChallengeData)
        {
            NTLMChallenge challenge = new NTLMChallenge();
            challenge.ServerChallenge = challenge.Challenge(Inveigh.Program.argChallenge);
            ChallengeData = challenge.ServerChallenge;
            byte[] timestamp = BitConverter.GetBytes(DateTime.Now.ToFileTime());
            NTLMAVPair ntlmAVPair = new NTLMAVPair();
            Inveigh.Program.dnsDomain = Inveigh.Program.computerName; // todo fix
            challenge.Payload = ntlmAVPair.GetBytes(Inveigh.Program.netbiosDomain, Inveigh.Program.computerName, Inveigh.Program.dnsDomain, Inveigh.Program.computerName, Inveigh.Program.dnsDomain, timestamp);
            byte[] challengeData = challenge.GetBytes(Inveigh.Program.computerName);
            //Console.WriteLine(BitConverter.ToString(challengeData));
            //GSSAPINTLMChallengeOld ntlmChallenge = new GSSAPINTLMChallengeOld();
            //byte[] test = ntlmChallenge.Test(challengeData);
            //GSSAPINTLMChallenge gssapi = new GSSAPINTLMChallenge();
            //byte[] test3 = test2.GetBytes(challengeData);

            byte[] gssapiData = challenge.Encode(challengeData);
            //Console.WriteLine(BitConverter.ToString(gssapiData));
            //byte[] ntlmChallengeData = ntlmChallenge.GetBytes(challengeData, ref ntlmChallenge);
            this.SecurityBufferLength = (ushort)gssapiData.Length;
            this.Buffer = gssapiData;
        }

    }
}
