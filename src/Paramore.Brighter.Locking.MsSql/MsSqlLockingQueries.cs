﻿namespace Paramore.Brighter.Locking.MsSql;

public static class MsSqlLockingQueries
{
    public const string ObtainLockQuery = "declare @result int; " +
                                          "Exec @result = sp_getapplock " +
                                          "@DbPrincipal = 'public' " +
                                          ",@Resource = @Resource" +
                                          ",@LockMode = 'Exclusive'" +
                                          ",@LockTimeout = @LockTimeout" +
                                          ",@LockOwner = 'Session'; " +
                                          "Select @result";

    public const string ReleaseLockQuery = "EXEC sp_releaseapplock  " +
                                           "@Resource = @Resource " +
                                           ",@DbPrincipal = 'public' " +
                                           ",@LockOwner = 'Session';";
}
