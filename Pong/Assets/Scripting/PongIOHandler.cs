// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Engine;
using CodeGamified.Time;
using Pong.Game;
using Pong.Scripting;
using UnityEngine;

namespace Pong.Scripting
{
    /// <summary>
    /// Game I/O handler for Pong — bridges CUSTOM opcodes to game state.
    /// </summary>
    public class PongIOHandler : IGameIOHandler
    {
        private readonly PongPaddle _paddle;
        private readonly PongPaddle _opponent;
        private readonly PongBall _ball;
        private readonly PongCourt _court;

        // Score references (set externally by PaddleProgram)
        public int PlayerScore { get; set; }
        public int OpponentScore { get; set; }

        public PongIOHandler(PongPaddle paddle, PongBall ball, PongCourt court)
        {
            _paddle = paddle;
            _ball = ball;
            _court = court;

            // Determine opponent
            _opponent = (_paddle.Side == PaddleSide.Left) ? _ball.RightPaddle : _ball.LeftPaddle;
        }

        public bool PreExecute(Instruction inst, MachineState state)
        {
            return true; // Pong has no crew gating
        }

        public void ExecuteIO(Instruction inst, MachineState state)
        {
            int pongOp = (int)inst.Op - (int)OpCode.CUSTOM_0;

            switch ((PongOpCode)pongOp)
            {
                // ── Queries → R0 ──
                case PongOpCode.GET_BALL_X:
                    state.SetRegister(0, _ball.Position.x);
                    break;
                case PongOpCode.GET_BALL_Y:
                    state.SetRegister(0, _ball.Position.y);
                    break;
                case PongOpCode.GET_BALL_VX:
                    state.SetRegister(0, _ball.Velocity.x);
                    break;
                case PongOpCode.GET_BALL_VY:
                    state.SetRegister(0, _ball.Velocity.y);
                    break;
                case PongOpCode.GET_PADDLE_Y:
                    state.SetRegister(0, _paddle.currentY);
                    break;
                case PongOpCode.GET_PADDLE_X:
                    state.SetRegister(0, _paddle.transform.position.x);
                    break;
                case PongOpCode.GET_SCORE:
                    state.SetRegister(0, PlayerScore);
                    break;
                case PongOpCode.GET_OPP_SCORE:
                    state.SetRegister(0, OpponentScore);
                    break;
                case PongOpCode.GET_OPP_Y:
                    state.SetRegister(0, _opponent != null ? _opponent.currentY : 0f);
                    break;
                case PongOpCode.GET_INPUT_Y:
                    state.SetRegister(0, PongInputProvider.Instance != null
                        ? PongInputProvider.Instance.VerticalInput : 0f);
                    break;
                case PongOpCode.GET_MOUSE_Y:
                    state.SetRegister(0, PongInputProvider.Instance != null
                        ? PongInputProvider.Instance.MouseWorldY : 0f);
                    break;

                // ── Orders ──
                case PongOpCode.SET_TARGET_Y:
                    float targetY = state.Registers[0];
                    _paddle.SetTargetY(targetY);
                    break;
                case PongOpCode.MOVE_TARGET_Y:
                    float delta = state.Registers[0];
                    _paddle.SetTargetY(_paddle.targetY + delta);
                    break;
            }
        }

        public float GetTimeScale()
        {
            return SimulationTime.Instance?.timeScale ?? 1f;
        }

        public double GetSimulationTime()
        {
            return SimulationTime.Instance?.simulationTime ?? 0.0;
        }
    }
}
