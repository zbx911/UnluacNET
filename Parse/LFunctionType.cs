﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnluacNET
{
    // TODO: Finish 'LFunctionType' class
    public class LFunctionType : BObjectType<LFunction>
    {
        public static readonly LFunctionType TYPE51 = new LFunctionType();
        public static readonly LFunctionType TYPE52 = new LFunctionType52();

        protected sealed class LFunctionParseState
        {
            public LString Name { get; set; }
            
            public int LineBegin { get; set; }
            public int LineEnd { get; set; }
            public int LenUpvalues { get; set; }
            public int LenParameter { get; set; }
            public int VarArg { get; set; }
            public int MaximumStackSize { get; set; }
            public int Length { get; set; }

            public int[] Code { get; set; }
            
            public BList<LObject> Constants { get; set; }
            public BList<LFunction> Functions { get; set; }
            public BList<BInteger> Lines { get; set; }
            public BList<LLocal> Locals { get; set; }
            
            public LUpvalue[] Upvalues { get; set; }
        }

        public override LFunction Parse(Stream stream, BHeader header)
        {
            if (header.Debug)
            {
                Console.WriteLine("-- beginning to parse function");
                Console.WriteLine("-- parsing name...start...end...upvalues...params...varargs...stack");
            }

            var s = new LFunctionParseState();

            ParseMain(stream, header, s);

            return new LFunction(header,
                s.Code,
                s.Locals.AsArray(),
                s.Constants.AsArray(),
                s.Upvalues,
                s.Functions.AsArray(),
                s.MaximumStackSize,
                s.LenUpvalues,
                s.LenParameter,
                s.VarArg);
        }

        protected virtual void ParseCode(Stream stream, BHeader header, LFunctionParseState s)
        {
            if (header.Debug)
                Console.WriteLine("-- beginning to parse bytecode list");

            s.Length = header.Integer.Parse(stream, header).AsInteger();
            s.Code = new int[s.Length];

            for (int i = 0; i < s.Length; i++)
            {
                s.Code[i] = stream.ReadInt32();

                if (header.Debug)
                    Console.WriteLine("-- parsed codepoint 0x{0:X}" + s.Code[i]);
            }
        }

        protected virtual void ParseConstants(Stream stream, BHeader header, LFunctionParseState s)
        {
            if (header.Debug)
                Console.WriteLine("-- beginning to parse constants list");

            s.Constants = header.Constant.ParseList(stream, header);

            if (header.Debug)
                Console.WriteLine("-- beginning to parse functions list");

            s.Functions = header.Function.ParseList(stream, header);
        }

        protected virtual void ParseDebug(Stream stream, BHeader header, LFunctionParseState s)
        {
            if (header.Debug)
                Console.WriteLine("-- beginning to parse source lines list");

            s.Lines = header.Integer.ParseList(stream, header);

            if (header.Debug)
                Console.WriteLine("-- beginning to parse upvalues list");

            var upvalNames = header.String.ParseList(stream, header);

            for (int i = 0; i < upvalNames.Length.AsInteger(); i++)
                s.Upvalues[i].Name = upvalNames[i].DeRef();
        }

        protected virtual void ParseMain(Stream stream, BHeader header, LFunctionParseState s)
        {
            s.Name = header.String.Parse(stream, header);

            s.LineBegin = header.Integer.Parse(stream, header).AsInteger();
            s.LineEnd = header.Integer.Parse(stream, header).AsInteger();

            s.LenUpvalues      = 0xFF & stream.ReadByte();
            s.LenParameter     = 0xFF & stream.ReadByte();
            s.VarArg           = 0xFF & stream.ReadByte();
            s.MaximumStackSize = 0xFF & stream.ReadByte();

            ParseCode(stream, header, s);
            ParseConstants(stream, header, s);
            ParseUpvalues(stream, header, s);
            ParseDebug(stream, header, s);
        }

        protected virtual void ParseUpvalues(Stream stream, BHeader header, LFunctionParseState s)
        {
            s.Upvalues = new LUpvalue[s.LenUpvalues];

            for (int i = 0; i < s.LenUpvalues; i++)
                s.Upvalues[i] = new LUpvalue();
        }
    }

    public class LFunctionType52 : LFunctionType
    {
        protected override void ParseDebug(Stream stream, BHeader header, LFunctionParseState s)
        {
            s.Name = header.String.Parse(stream, header);

            base.ParseDebug(stream, header, s);
        }

        protected override void ParseMain(Stream stream, BHeader header, LFunctionParseState s)
        {
            s.LineBegin = header.Integer.Parse(stream, header).AsInteger();
            s.LineEnd = header.Integer.Parse(stream, header).AsInteger();

            s.LenParameter     = 0xFF & stream.ReadByte();
            s.VarArg           = 0xFF & stream.ReadByte();
            s.MaximumStackSize = 0xFF & stream.ReadByte();

            ParseCode(stream, header, s);
            ParseConstants(stream, header, s);
            ParseUpvalues(stream, header, s);
            ParseDebug(stream, header, s);
        }

        protected override void ParseUpvalues(Stream stream, BHeader header, LFunctionParseState s)
        {
            var upvalues = header.UpValue.ParseList(stream, header);

            s.LenUpvalues = upvalues.Length.AsInteger();
            s.Upvalues    = upvalues.AsArray();
        }
    }
}
