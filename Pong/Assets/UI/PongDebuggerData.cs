// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Runtime;
using CodeGamified.TUI;
using Pong.Scripting;
using static Pong.Scripting.PongOpCode;

namespace Pong.UI
{
    /// <summary>
    /// Adapts a PaddleProgram into the engine's IDebuggerDataSource contract.
    /// Fed to DebuggerSourcePanel, DebuggerMachinePanel, DebuggerStatePanel.
    /// </summary>
    public class PongDebuggerData : IDebuggerDataSource
    {
        private readonly Pong.Scripting.PaddleProgram _program;
        private readonly string _label;

        public PongDebuggerData(Pong.Scripting.PaddleProgram program, string label = null)
        {
            _program = program;
            _label = label;
        }

        public string ProgramName => _label ?? _program?.ProgramName ?? "PaddleAI";
        public string[] SourceLines => _program?.Program?.SourceLines;
        public bool HasLiveProgram =>
            _program != null && _program.Executor != null && _program.Program != null
            && _program.Program.Instructions != null && _program.Program.Instructions.Length > 0;
        public int PC => _program?.State?.PC ?? 0;
        public long CycleCount => _program?.State?.CycleCount ?? 0;

        public string StatusString
        {
            get
            {
                if (_program == null || _program.Executor == null)
                    return TUIColors.Dimmed("NO PROGRAM");
                var state = _program.State;
                if (state == null) return TUIColors.Dimmed("NO STATE");
                int instCount = _program.Program?.Instructions?.Length ?? 0;
                return TUIColors.Fg(TUIColors.BrightGreen, $"TICK {instCount} inst");
            }
        }

        public List<string> BuildSourceLines(int pc, int scrollOffset, int maxRows)
        {
            var lines = new List<string>();
            var src = SourceLines;
            if (src == null) return lines;

            int activeLine = -1;
            if (HasLiveProgram && _program.Program.Instructions.Length > 0
                && pc < _program.Program.Instructions.Length)
                activeLine = _program.Program.Instructions[pc].SourceLine - 1;

            for (int i = scrollOffset; i < src.Length && lines.Count < maxRows; i++)
            {
                bool isActive = (i == activeLine);
                if (isActive)
                {
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $" {i + 1,3} {src[i]}"));
                }
                else
                {
                    string num = TUIColors.Dimmed($"{i + 1,3}");
                    lines.Add($" {num} {src[i]}");
                }
            }
            return lines;
        }

        public List<string> BuildMachineLines(int pc, int maxRows)
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var instructions = _program.Program.Instructions;
            int total = instructions.Length;
            int offset = pc - maxRows / 3;
            int visibleCount = Mathf.Min(maxRows, total + 2);

            for (int j = 0; j < visibleCount; j++)
            {
                int i = ((offset + j) % total + total) % total;
                var inst = instructions[i];
                bool isPC = (i == pc);
                string asm = inst.ToAssembly(FormatPongOp);
                if (isPC)
                {
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $" {i:X3} {asm}"));
                }
                else
                {
                    string addr = TUIColors.Dimmed($"{i:X3}");
                    lines.Add($" {addr} {asm}");
                }
            }
            return lines;
        }

        public List<string> BuildStateLines()
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var state = _program.State;

            for (int r = 0; r < MachineState.REGISTER_COUNT; r++)
            {
                bool modified = (r == state.LastRegisterModified);
                string rName = $"R{r}:";
                string rVal = $"{state.Registers[r]:F2}";
                if (modified)
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $" {rName,-4} {rVal}"));
                else
                    lines.Add($" {TUIColors.Dimmed(rName),-4} {rVal}");
            }

            lines.Add(TUIColors.Dimmed(TUIWidgets.Divider(13)));

            lines.Add($" FLAGS: {state.Flags}");
            lines.Add($" PC: {state.PC}");
            lines.Add($" STACK [{state.Stack.Count}]");

            if (state.NameToAddress.Count > 0)
            {
                lines.Add(TUIColors.Dimmed(TUIWidgets.Divider(13)));
                lines.Add(TUIColors.Fg(TUIColors.BrightCyan, " VARIABLES"));
                foreach (var kvp in state.NameToAddress)
                {
                    string name = kvp.Key;
                    float val = state.Memory.ContainsKey(name) ? state.Memory[name] : 0;
                    lines.Add($" {TUIColors.Dimmed(name + ":")} {val:F2}");
                }
            }

            return lines;
        }

        static string FormatPongOp(Instruction inst)
        {
            int id = (int)inst.Op - (int)OpCode.CUSTOM_0;
            return (PongOpCode)id switch
            {
                GET_BALL_X    => "INP  R0, BALL.X",
                GET_BALL_Y    => "INP  R0, BALL.Y",
                GET_BALL_VX   => "INP  R0, BALL.VX",
                GET_BALL_VY   => "INP  R0, BALL.VY",
                GET_PADDLE_Y  => "INP  R0, PAD.Y",
                GET_PADDLE_X  => "INP  R0, PAD.X",
                GET_SCORE     => "INP  R0, SCORE",
                GET_OPP_SCORE => "INP  R0, OPP.SC",
                GET_OPP_Y     => "INP  R0, OPP.Y",
                SET_TARGET_Y  => "OUT  TGT.Y, R0",
                _             => $"IO.{id,2} {inst.Arg0}, {inst.Arg1}"
            };
        }
    }
}
