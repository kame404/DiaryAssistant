using DiaryAssistant.Models;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DiaryAssistant.Services
{
    // データベースサービス
    public static class DatabaseService
    {
        private static string _dbPath;
        private static readonly object _lockObject = new object();
        private static readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);
        private static LiteDatabase _sharedDbInstance;
        private static bool _isInitialized = false;

        public static void Initialize(string dbPath)
        {
            lock (_lockObject)
            {
                if (_isInitialized)
                {
                    return;
                }

                _dbPath = dbPath;

                try
                {
                    // データベースフォルダの存在確認
                    string dbDirectory = Path.GetDirectoryName(dbPath);
                    if (!Directory.Exists(dbDirectory))
                    {
                        Directory.CreateDirectory(dbDirectory);
                    }

                    // 共有DB接続を初期化
                    InitializeSharedConnection();

                    // データベースが存在しない場合は初期化
                    if (!File.Exists(dbPath))
                    {
                        using (var db = GetDatabaseConnection())
                        {
                            // 設定コレクションの作成
                            var settings = db.GetCollection<AppSettings>("settings");
                            settings.EnsureIndex(x => x.Id);

                            // 日記コレクションの作成
                            var entries = db.GetCollection<DiaryEntry>("diary_entries");
                            entries.EnsureIndex(x => x.Date);
                        }
                    }

                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"データベース初期化エラー: {ex.Message}");
                    throw;
                }
            }
        }

        private static void InitializeSharedConnection()
        {
            try
            {
                _dbSemaphore.Wait();

                if (_sharedDbInstance != null)
                {
                    _sharedDbInstance.Dispose();
                }

                // LiteDB 接続オプションの構成
                var connectionString = new ConnectionString
                {
                    Filename = _dbPath,
                    Connection = ConnectionType.Shared,  // 共有モードで接続
                    ReadOnly = false,
                    Upgrade = true,     // 必要に応じてアップグレード
                    Collation = new Collation("ja-JP")  // 日本語のソート順をサポート
                };

                _sharedDbInstance = new LiteDatabase(connectionString);
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        // データベース接続を取得（既存の共有接続を返すか、新しい接続を作成）
        private static LiteDatabase GetDatabaseConnection()
        {
            // 共有インスタンスがある場合はそれを使用
            if (_sharedDbInstance != null)
            {
                return _sharedDbInstance;
            }

            // ない場合は新しい接続を作成（通常はInitializeで作成されるはず）
            var connectionString = new ConnectionString
            {
                Filename = _dbPath,
                Connection = ConnectionType.Shared
            };

            return new LiteDatabase(connectionString);
        }

        // アプリケーション終了時の後片付け
        public static void Cleanup()
        {
            try
            {
                _dbSemaphore.Wait();

                if (_sharedDbInstance != null)
                {
                    _sharedDbInstance.Dispose();
                    _sharedDbInstance = null;
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        // 設定の取得
        public static AppSettings GetSettings()
        {
            try
            {
                _dbSemaphore.Wait();

                var db = GetDatabaseConnection();
                var settings = db.GetCollection<AppSettings>("settings");
                return settings.FindOne(x => x.Id == 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"設定取得エラー: {ex.Message}");
                return null;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        // 設定の保存
        public static void SaveSettings(AppSettings settings)
        {
            if (settings == null) return;

            try
            {
                _dbSemaphore.Wait();

                var db = GetDatabaseConnection();
                var settingsCollection = db.GetCollection<AppSettings>("settings");
                settingsCollection.Upsert(settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"設定保存エラー: {ex.Message}");
                throw;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        // 日記の取得（特定の日付）
        public static DiaryEntry GetDiaryEntry(DateTime date)
        {
            date = date.Date; // 時間部分を0に設定

            try
            {
                _dbSemaphore.Wait();

                var db = GetDatabaseConnection();
                var entries = db.GetCollection<DiaryEntry>("diary_entries");
                return entries.FindOne(x => x.Date == date);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日記取得エラー: {ex.Message}");
                return null;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        // すべての日記エントリを取得
        public static List<DiaryEntry> GetAllDiaryEntries()
        {
            try
            {
                _dbSemaphore.Wait();

                var db = GetDatabaseConnection();
                var entries = db.GetCollection<DiaryEntry>("diary_entries");
                return entries.FindAll().OrderByDescending(x => x.Date).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日記一覧取得エラー: {ex.Message}");
                return new List<DiaryEntry>();
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        // 日記の保存/更新
        public static void SaveDiaryEntry(DiaryEntry entry)
        {
            if (entry == null) return;

            entry.LastModified = DateTime.Now;

            try
            {
                _dbSemaphore.Wait();

                var db = GetDatabaseConnection();
                var entries = db.GetCollection<DiaryEntry>("diary_entries");

                // 既存のエントリを検索
                var existingEntry = entries.FindOne(x => x.Date == entry.Date.Date);

                if (existingEntry != null)
                {
                    // 既存エントリの更新
                    entry.Id = existingEntry.Id;
                    entries.Update(entry);
                }
                else
                {
                    // 新規エントリの挿入
                    entries.Insert(entry);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日記保存エラー: {ex.Message}");
                throw;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        // 過去のイベントを追記
        public static void AppendToPastEvent(DateTime date, ConversationMessage message)
        {
            if (message == null) return;

            date = date.Date; // 時間部分を0に設定

            try
            {
                _dbSemaphore.Wait();

                var db = GetDatabaseConnection();
                var entries = db.GetCollection<DiaryEntry>("diary_entries");
                var entry = entries.FindOne(x => x.Date == date);

                if (entry != null)
                {
                    // 既存のエントリに追記
                    entry.Conversation.Add(message);
                    entry.LastModified = DateTime.Now;
                    entries.Update(entry);
                }
                else
                {
                    // 新規エントリを作成
                    var newEntry = new DiaryEntry
                    {
                        Date = date,
                        Conversation = new List<ConversationMessage> { message },
                        LastModified = DateTime.Now
                    };
                    entries.Insert(newEntry);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"過去イベント追記エラー: {ex.Message}");
                throw;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        // 指定期間の日記エントリを取得
        public static List<DiaryEntry> GetDiaryEntriesByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                _dbSemaphore.Wait();

                var db = GetDatabaseConnection();
                var entries = db.GetCollection<DiaryEntry>("diary_entries");

                return entries.Find(x => x.Date >= startDate.Date && x.Date <= endDate.Date)
                              .OrderByDescending(x => x.Date)
                              .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日記範囲取得エラー: {ex.Message}");
                return new List<DiaryEntry>();
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        // データベースの最適化を実行
        public static bool OptimizeDatabase()
        {
            try
            {
                _dbSemaphore.Wait();

                // 既存の接続を一度閉じる
                if (_sharedDbInstance != null)
                {
                    _sharedDbInstance.Dispose();
                    _sharedDbInstance = null;
                }

                // 排他アクセスモードで一時的に接続
                using (var db = new LiteDatabase(_dbPath))
                {
                    // データベースを再構築
                    db.Rebuild();

                    // コレクションの断片化解消
                    db.Checkpoint();
                }

                // 共有接続を再初期化
                InitializeSharedConnection();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"データベース最適化エラー: {ex.Message}");

                // エラー発生時も共有接続の再初期化を試みる
                try
                {
                    InitializeSharedConnection();
                }
                catch { }

                return false;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
    }
}