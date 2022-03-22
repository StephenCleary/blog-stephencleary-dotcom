---
layout: post
title: "TransactionScope Has a Default Timeout"
---
I've been experimenting with using Entity Framework 4.0 hooked up to [System.Data.SQLite](http://sqlite.phxsoftware.com/), an ADO.NET provider for [SQLite](http://www.sqlite.org/). I ran into an interesting problem last night when a long-running database operation would suddenly abort the process. If run in the debugger, the application would abort (without an error or exception raised). If run outside the debugger, the application would bring up a Windows Error Reporting dialog.

It's possible to copy the captured minidump while the WER dialog is being shown. When this was loaded into Visual Studio, it was revealed that the crash was due to a System.Data.SQLLite.SQLiteException thrown with the error message "The database file is locked" (SQLITE_BUSY; note that this is different than SQLITE_LOCKED).

Running the program with [DebugView](http://technet.microsoft.com/en-us/sysinternals/bb896647.aspx?WT.mc_id=DT-MVP-5000058) showed more clearly what was happening. The [TransactionScope](http://msdn.microsoft.com/en-us/library/system.transactions.transactionscope.aspx?WT.mc_id=DT-MVP-5000058) class uses a default timeout ([TransactionManager.DefaultTimeout](http://msdn.microsoft.com/en-us/library/system.transactions.transactionmanager.defaulttimeout.aspx?WT.mc_id=DT-MVP-5000058), which has a default value of 1 minute). When the transaction timed out, it attempted to issue an abort command (SQLiteTransaction.Rollback -> SQLiteCommand.ExecuteNonQuery). However, this command is issued from a timer callback (on a threadpool thread), so SQLite rejects the rollback operation because another thread is still using those tables.

Passing TimeSpan.Zero into the TransactionScope constructor will instruct it to use the maximum timeout (TransactionManager.MaximumTimeout, which has a default value of 10 minutes) instead of the default. Unfortunately, the maximum timeout can only be increased by editing the machine.config file.

