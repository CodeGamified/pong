// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections.Generic;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;

namespace Pong.Scripting
{
    /// <summary>
    /// Pong-specific opcodes mapped to CUSTOM_0..CUSTOM_N.
    /// These are the I/O operations available to player scripts.
    /// </summary>
    public enum PongOpCode
    {
        // Queries (read game state into register)
        GET_BALL_X     = 0,  // CUSTOM_0
        GET_BALL_Y     = 1,  // CUSTOM_1
        GET_BALL_VX    = 2,  // CUSTOM_2
        GET_BALL_VY    = 3,  // CUSTOM_3
        GET_PADDLE_Y   = 4,  // CUSTOM_4
        GET_PADDLE_X   = 5,  // CUSTOM_5
        GET_SCORE      = 6,  // CUSTOM_6
        GET_OPP_SCORE  = 7,  // CUSTOM_7
        GET_OPP_Y      = 8,  // CUSTOM_8
        GET_INPUT_Y    = 9,  // CUSTOM_9

        // Orders (write to game state)
        SET_TARGET_Y   = 10, // CUSTOM_10

        // More queries
        GET_MOUSE_Y    = 11, // CUSTOM_11

        // More orders
        MOVE_TARGET_Y  = 12, // CUSTOM_12

        // Court dimensions
        GET_COURT_H    = 13, // CUSTOM_13
        GET_COURT_W    = 14, // CUSTOM_14

        // Event waits
        WAIT_OPP_HIT   = 15, // CUSTOM_15
        WAIT_WALL_HIT  = 16, // CUSTOM_16
    }

    /// <summary>
    /// Compiler extension for Pong — registers builtins like get_ball_y(), set_target_y().
    /// </summary>
    public class PongCompilerExtension : ICompilerExtension
    {
        public void RegisterBuiltins(CompilerContext ctx)
        {
            // No special types for Pong (yet)
        }

        public bool TryCompileCall(string functionName, List<AstNodes.ExprNode> args,
                                   CompilerContext ctx, int sourceLine)
        {
            switch (functionName)
            {
                // ── Queries: result stored in R0 ──
                case "get_ball_x":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_BALL_X, 0, 0, 0, sourceLine, "get_ball_x → R0");
                    return true;
                case "get_ball_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_BALL_Y, 0, 0, 0, sourceLine, "get_ball_y → R0");
                    return true;
                case "get_ball_vx":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_BALL_VX, 0, 0, 0, sourceLine, "get_ball_vx → R0");
                    return true;
                case "get_ball_vy":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_BALL_VY, 0, 0, 0, sourceLine, "get_ball_vy → R0");
                    return true;
                case "get_paddle_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_PADDLE_Y, 0, 0, 0, sourceLine, "get_paddle_y → R0");
                    return true;
                case "get_paddle_x":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_PADDLE_X, 0, 0, 0, sourceLine, "get_paddle_x → R0");
                    return true;
                case "get_score":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_SCORE, 0, 0, 0, sourceLine, "get_score → R0");
                    return true;
                case "get_opponent_score":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_OPP_SCORE, 0, 0, 0, sourceLine, "get_opponent_score → R0");
                    return true;
                case "get_opponent_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_OPP_Y, 0, 0, 0, sourceLine, "get_opponent_y → R0");
                    return true;
                case "get_input_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_INPUT_Y, 0, 0, 0, sourceLine, "get_input_y → R0");
                    return true;
                case "get_mouse_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_MOUSE_Y, 0, 0, 0, sourceLine, "get_mouse_y → R0");
                    return true;
                case "get_court_height":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_COURT_H, 0, 0, 0, sourceLine, "get_court_height → R0");
                    return true;
                case "get_court_width":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.GET_COURT_W, 0, 0, 0, sourceLine, "get_court_width → R0");
                    return true;
                case "wait_for_opponent_hit":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.WAIT_OPP_HIT, 0, 0, 0, sourceLine, "wait_for_opponent_hit");
                    return true;
                case "wait_for_wall_hit":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.WAIT_WALL_HIT, 0, 0, 0, sourceLine, "wait_for_wall_hit");
                    return true;

                // ── Orders: arg from R0 ──
                case "set_target_y":
                    if (args != null && args.Count > 0)
                        args[0].Compile(ctx);  // Result in R0
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.SET_TARGET_Y, 0, 0, 0, sourceLine, "set_target_y(R0)");
                    return true;
                case "move_target_y":
                    if (args != null && args.Count > 0)
                        args[0].Compile(ctx);  // Result in R0
                    ctx.Emit(OpCode.CUSTOM_0 + (int)PongOpCode.MOVE_TARGET_Y, 0, 0, 0, sourceLine, "move_target_y(R0)");
                    return true;

                default:
                    return false;
            }
        }

        public bool TryCompileMethodCall(string objectName, string methodName,
                                         List<AstNodes.ExprNode> args,
                                         CompilerContext ctx, int sourceLine)
        {
            return false; // No objects in Pong (yet)
        }

        public bool TryCompileObjectDecl(string typeName, string varName,
                                         List<AstNodes.ExprNode> constructorArgs,
                                         CompilerContext ctx, int sourceLine)
        {
            return false;
        }
    }
}
