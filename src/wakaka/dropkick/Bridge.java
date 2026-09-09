package wakaka.dropkick;

import se.krka.kahlua.integration.annotations.LuaMethod;
import zombie.characters.IsoZombie;
import zombie.core.physics.Bullet;
import zombie.core.physics.RagdollController;
import zombie.iso.IsoCell;
import zombie.iso.objects.IsoZombieGiblets;
import zombie.network.GameClient;
import zombie.network.GameServer;

public final class Bridge {
    private static boolean singlePlayer() { return !GameClient.client && !GameServer.server; }

    @LuaMethod(name="dropKickPrivateBridgeVersion", global=true)
    public static String version() { return "1"; }

    @LuaMethod(name="dropKickPrivateImpulse", global=true)
    public static boolean impulse(IsoZombie zombie, float x, float y, float up) {
        if (!singlePlayer() || zombie == null || !zombie.isRagdollSimulationActive()) return false;
        if (!Float.isFinite(x) || !Float.isFinite(y) || !Float.isFinite(up)) return false;
        RagdollController controller = zombie.getRagdollController();
        if (controller == null) return false;
        Bullet.applyImpulse(controller.getID(), 0, new float[] {x, up, y, 0, 0, 0});
        return true;
    }

    @LuaMethod(name="dropKickPrivateBlood", global=true)
    public static void blood(IsoCell cell, float x, float y, float z, float vx, float vy) {
        if (!singlePlayer() || cell == null) return;
        if (!Float.isFinite(x) || !Float.isFinite(y) || !Float.isFinite(z)
                || !Float.isFinite(vx) || !Float.isFinite(vy)) return;
        new IsoZombieGiblets(IsoZombieGiblets.GibletType.A, cell, x, y, z, vx, vy);
    }
}
