// Copyright CodeGamified 2025-2026
// MIT License — Pong: Hello World
using CodeGamified.Persistence;
using Pong.Scripting;
using UnityEngine;

namespace Pong.Persistence
{
    /// <summary>
    /// Autosave manager for Pong — persists player scripts via the Persistence framework.
    /// Uses MemoryGitProvider by default (in-memory). Swap to LocalGitProvider
    /// when .data/ submodule is configured.
    /// </summary>
    public class PongPersistenceManager : PersistenceBehaviour
    {
        private EntityStore<PongScriptData> _scriptStore;
        private PaddleProgram _player;
        private string _playerId = "local";
        private string _lastSavedSource;

        public void Initialize(IGitRepository repo, PaddleProgram player)
        {
            base.Initialize(repo);
            _player = player;
            _scriptStore = new EntityStore<PongScriptData>(
                repo, new PongScriptSerializer(), "scripts");

            // Try to load existing script
            var existing = _scriptStore.Load(_playerId, "PaddleAI");
            if (existing != null && !string.IsNullOrEmpty(existing.source))
            {
                _player.UploadCode(existing.source);
                _lastSavedSource = existing.source;
                Debug.Log("[Persistence] Loaded saved script");
            }
        }

        protected override GitResult PerformSave(IGitRepository repo)
        {
            if (_player == null) return GitResult.Ok();

            string currentSource = _player.CurrentSourceCode;
            if (currentSource == _lastSavedSource) return GitResult.Ok();

            var data = new PongScriptData
            {
                name = _player.ProgramName ?? "PaddleAI",
                source = currentSource,
                tier = 0,
                bestScore = 0f
            };

            var result = _scriptStore.Save(_playerId, data.name, data, $"autosave: {data.name}");
            if (result.Success)
                _lastSavedSource = currentSource;

            return result;
        }

        /// <summary>Mark dirty when player uploads new code.</summary>
        public void OnCodeUploaded()
        {
            MarkDirty();
        }
    }
}
