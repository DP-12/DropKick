package wakaka.dropkick;

import zombie.Lua.LuaManager;

public final class Bootstrap {
    public static void install() {
        // A separate name prevents an old patched GlobalObject from passing our check.
        LuaManager.exposer.exposeGlobalFunctions(new Bridge());
        System.out.println("[DropKickLoader] private bridge registered: 1");
    }
}
