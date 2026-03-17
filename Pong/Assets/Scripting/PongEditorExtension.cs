// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using System.Collections.Generic;
using CodeGamified.Editor;
using CodeGamified.Engine.Compiler;

namespace Pong.Scripting
{
    /// <summary>
    /// Editor extension for Pong — provides game-specific options
    /// to CodeEditorWindow's option tree.
    /// Exposes Pong builtins as available functions for tap-to-code editing.
    /// </summary>
    public class PongEditorExtension : IEditorExtension
    {
        public List<EditorTypeInfo> GetAvailableTypes()
        {
            return new List<EditorTypeInfo>(); // No object types in Pong
        }

        public List<EditorFuncInfo> GetAvailableFunctions()
        {
            return new List<EditorFuncInfo>
            {
                new EditorFuncInfo { Name = "get_ball_x",       Hint = "ball X position",      ArgCount = 0 },
                new EditorFuncInfo { Name = "get_ball_y",       Hint = "ball Y position",      ArgCount = 0 },
                new EditorFuncInfo { Name = "get_ball_vx",      Hint = "ball X velocity",      ArgCount = 0 },
                new EditorFuncInfo { Name = "get_ball_vy",      Hint = "ball Y velocity",      ArgCount = 0 },
                new EditorFuncInfo { Name = "get_paddle_y",     Hint = "your paddle Y",        ArgCount = 0 },
                new EditorFuncInfo { Name = "get_paddle_x",     Hint = "your paddle X",        ArgCount = 0 },
                new EditorFuncInfo { Name = "get_opponent_y",   Hint = "opponent paddle Y",    ArgCount = 0 },
                new EditorFuncInfo { Name = "get_score",        Hint = "your score",           ArgCount = 0 },
                new EditorFuncInfo { Name = "get_opponent_score",Hint = "opponent score",      ArgCount = 0 },
                new EditorFuncInfo { Name = "get_court_height", Hint = "court height",         ArgCount = 0 },
                new EditorFuncInfo { Name = "get_court_width",  Hint = "court width",          ArgCount = 0 },
                new EditorFuncInfo { Name = "wait_for_opponent_hit", Hint = "sleep until opponent hits ball", ArgCount = 0 },
                new EditorFuncInfo { Name = "wait_for_wall_hit",     Hint = "sleep until ball bounces off wall", ArgCount = 0 },
                new EditorFuncInfo { Name = "set_target_y",     Hint = "move paddle to Y",     ArgCount = 1 },
            };
        }

        public List<EditorMethodInfo> GetMethodsForType(string typeName)
        {
            return new List<EditorMethodInfo>(); // No object methods in Pong
        }

        public List<string> GetVariableNameSuggestions()
        {
            return new List<string>
            {
                "ball_y", "ball_x", "ball_vy", "ball_vx",
                "target", "predicted_y", "offset", "t",
                "opp_y", "my_y"
            };
        }

        public List<string> GetStringLiteralSuggestions()
        {
            return new List<string>(); // No string builtins in Pong
        }
    }
}
