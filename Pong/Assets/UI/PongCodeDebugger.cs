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
    /// Unified code debugger — works for ANY PaddleProgram (player OR AI).
    /// Three-panel live view: SOURCE CODE │ MACHINE CODE │ REGISTERS & STATE
    /// Two instances created by PongTUIManager, one per paddle.
    /// </summary>
    public class PongCodeDebugger : CodeDebuggerWindow
    {
        private PaddleProgram _program;

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "CODE";
        }

        public void SetTitle(string title)
        {
            windowTitle = title;
        }

        public void Bind(PaddleProgram program)
        {
            _program = program;
        }

        protected override string[] GetSourceLines()
        {
            return _program?.Program?.SourceLines;
        }

        protected override string GetProgramName()
        {
            return _program?.ProgramName ?? "PaddleAI";
        }

        protected override bool HasLiveProgram =>
            _program != null && _program.Executor != null && _program.Program != null
            && _program.Program.Instructions != null && _program.Program.Instructions.Length > 0;

        protected override int GetPC() =>
            _program?.State?.PC ?? 0;

        protected override long GetCycleCount() =>
            _program?.State?.CycleCount ?? 0;

        protected override string GetStatusString()
        {
            if (_program == null || _program.Executor == null)
                return TUIColors.Dimmed("NO PROGRAM");

            var state = _program.State;
            if (state == null) return TUIColors.Dimmed("NO STATE");
            // HALT is normal — tick-based execution halts at end of script each tick
            int instCount = _program.Program?.Instructions?.Length ?? 0;
            return TUIColors.Fg(TUIColors.BrightGreen, $"TICK {instCount} inst");
        }

        protected override List<string> BuildSourceColumn(int pc)
        {
            var lines = new List<string>();
            var src = GetSourceLines();
            if (src == null) return lines;

            int activeLine = -1;
            if (HasLiveProgram && _program.Program.Instructions.Length > 0 && pc < _program.Program.Instructions.Length)
                activeLine = _program.Program.Instructions[pc].SourceLine - 1;

            for (int i = scrollOffset; i < src.Length && lines.Count < ContentRows; i++)
            {
                bool isActive = (i == activeLine);
                string prefix = " ";
                string num = TUIColors.Dimmed($"{i + 1,3}");
                string text = isActive
                    ? TUIColors.Fg(TUIColors.BrightGreen, src[i])
                    : src[i];
                lines.Add($"{prefix}{num} {text}");
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

        protected override List<string> BuildAsmColumn(int pc)
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var instructions = _program.Program.Instructions;
            int total = instructions.Length;
            int offset = pc - ContentRows / 3;
            int visibleCount = Mathf.Min(ContentRows, total + 2);

            for (int j = 0; j < visibleCount; j++)
            {
                int i = ((offset + j) % total + total) % total;
                var inst = instructions[i];
                bool isPC = (i == pc);
                string addr = TUIColors.Dimmed($"{i:X3}");
                string asm = inst.ToAssembly(FormatPongOp);
                string text = isPC
                    ? TUIColors.Fg(TUIColors.BrightCyan, asm)
                    : asm;
                lines.Add($" {addr} {text}");
            }
            return lines;
        }

        protected override List<string> BuildStateColumn()
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var state = _program.State;

            // Registers
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

            lines.Add(Separator(col3Start > 0 ? totalChars - col3Start - 5 : 13));

            // Flags
            lines.Add($" FLAGS: {state.Flags}");
            lines.Add($" PC: {state.PC}");
            lines.Add($" STACK [{state.Stack.Count}]");

            // Variables
            if (state.NameToAddress.Count > 0)
            {
                lines.Add(Separator(col3Start > 0 ? totalChars - col3Start - 5 : 13));
                lines.Add(TUIColors.Fg(TUIColors.BrightCyan, " VARIABLES"));
                foreach (var kvp in state.NameToAddress)
                {
                    string name = kvp.Key;
                    float val = 0;
                    if (state.Memory.ContainsKey(name))
                        val = state.Memory[name];
                    lines.Add($" {TUIColors.Dimmed(name + ":")} {val:F2}");
                }
            }

            return lines;
        }
    }
}
