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
        public int PC
        {
            get
            {
                var s = _program?.State;
                if (s == null) return 0;
                return s.LastExecutedPC >= 0 ? s.LastExecutedPC : s.PC;
            }
        }
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
            int activeEnd = -1;
            bool isHalt = false;
            Instruction activeInst = default;
            if (HasLiveProgram && _program.Program.Instructions.Length > 0
                && pc < _program.Program.Instructions.Length)
            {
                activeInst = _program.Program.Instructions[pc];
                activeLine = activeInst.SourceLine - 1;
                isHalt = activeInst.Op == OpCode.HALT;
                if (activeLine >= 0)
                    activeEnd = SourceHighlight.GetContinuationEnd(src, activeLine);
            }

            // Synthetic "while True:" at display row 0
            if (scrollOffset == 0 && lines.Count < maxRows)
            {
                string whileLine = "while True:";
                if (isHalt)
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $"  {TUIGlyphs.ArrowR}   {whileLine}"));
                else
                    lines.Add($"  {TUIColors.Dimmed(TUIGlyphs.ArrowR)}   {SynthwaveHighlighter.Highlight(whileLine)}");
            }

            // Find the ONE line that contains the active token
            int tokenLine = -1;
            if (activeLine >= 0)
            {
                string token = SourceHighlight.GetSourceToken(activeInst);
                if (token != null)
                {
                    for (int k = activeLine; k <= activeEnd; k++)
                    {
                        if (src[k].IndexOf(token) >= 0) { tokenLine = k; break; }
                    }
                }
                if (tokenLine < 0) tokenLine = activeLine; // fallback: first line
            }

            for (int i = scrollOffset; i < src.Length && lines.Count < maxRows; i++)
            {
                if (i == tokenLine)
                {
                    lines.Add(SourceHighlight.HighlightActiveLine(
                        src[i], $" {i + 1:D3}      ", activeInst));
                }
                else
                {
                    string num = TUIColors.Dimmed($"{i + 1:D3}");
                    lines.Add($" {num}      {SynthwaveHighlighter.Highlight(src[i])}");
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

            // Scroll window so PC is always visible
            int offset = 0;
            if (total > maxRows)
                offset = Mathf.Clamp(pc - maxRows / 3, 0, total - maxRows);
            int visibleCount = Mathf.Min(maxRows, total);

            for (int j = 0; j < visibleCount; j++)
            {
                int i = offset + j;
                var inst = instructions[i];
                bool isPC = (i == pc);
                string asm = inst.ToAssembly(FormatPongOp);
                if (isPC)
                {
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $" {i:X3}  {asm}"));
                }
                else
                {
                    string addr = TUIColors.Dimmed($"{i:X3}");
                    lines.Add($" {addr}  {SynthwaveHighlighter.HighlightAsm(asm)}");
                }
            }
            return lines;
        }

        public List<string> BuildStateLines()
        {
            if (!HasLiveProgram) return new List<string>();
            var s = _program.State;
            int displayPC = s.LastExecutedPC >= 0 ? s.LastExecutedPC : s.PC;
            return TUIWidgets.BuildStateLines(
                s.Registers, s.LastRegisterModified,
                s.Flags, displayPC, s.Stack.Count,
                s.NameToAddress, s.Memory);
        }

        static string FormatPongOp(Instruction inst)
        {
            int id = (int)inst.Op - (int)OpCode.CUSTOM_0;
            return (PongOpCode)id switch
            {
                GET_BALL_X    => "INP R0, BALL.X",
                GET_BALL_Y    => "INP R0, BALL.Y",
                GET_BALL_VX   => "INP R0, BALL.VX",
                GET_BALL_VY   => "INP R0, BALL.VY",
                GET_PADDLE_Y  => "INP R0, PAD.Y",
                GET_PADDLE_X  => "INP R0, PAD.X",
                GET_SCORE     => "INP R0, SCORE",
                GET_OPP_SCORE => "INP R0, OPP.SC",
                GET_OPP_Y     => "INP R0, OPP.Y",
                GET_COURT_H   => "INP R0, CRT.H",
                GET_COURT_W   => "INP R0, CRT.W",
                WAIT_OPP_HIT  => "WAIT OPP.HIT",
                WAIT_WALL_HIT => "WAIT WALL.HIT",
                SET_TARGET_Y  => "OUT TGT.Y, R0",
                _             => $"IO.{id,2} {inst.Arg0}, {inst.Arg1}"
            };
        }
    }
}
