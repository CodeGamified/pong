// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.TUI;
using Pong.Scripting;

namespace Pong.UI
{
    /// <summary>
    /// Thin adapter — wires a PaddleProgram into the engine's CodeDebuggerWindow
    /// via PongDebuggerData (IDebuggerDataSource). All rendering lives in the engine.
    /// </summary>
    public class PongCodeDebugger : CodeDebuggerWindow
    {
        protected override void Awake()
        {
            base.Awake();
            windowTitle = "CODE";
        }

        public void Bind(PaddleProgram program)
        {
            SetDataSource(new PongDebuggerData(program));
        }
    }
}
