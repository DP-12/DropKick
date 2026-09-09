package wakaka.dropkick;

import java.io.InputStream;
import java.lang.classfile.ClassFile;
import java.lang.classfile.ClassTransform;
import java.lang.classfile.Opcode;
import java.lang.classfile.instruction.ReturnInstruction;
import java.lang.constant.ClassDesc;
import java.lang.constant.MethodTypeDesc;
import java.lang.instrument.ClassFileTransformer;
import java.lang.instrument.Instrumentation;
import java.security.MessageDigest;
import java.security.ProtectionDomain;
import java.util.HexFormat;
import java.util.Properties;

/** Private DropKick-only agent. No disk patches, scanning, or retransformation. */
public final class Agent implements ClassFileTransformer {
    private static final String TARGET = "zombie/Lua/LuaManager";
    private static final Properties COMPATIBILITY = new Properties();
    private static boolean disabled;

    public static void premain(String args, Instrumentation instrumentation) {
        try {
            if (Runtime.version().feature() != 25) throw new IllegalStateException("requires Java 25");
            try (InputStream in = Agent.class.getResourceAsStream("/compatibility.properties")) {
                if (in == null) throw new IllegalStateException("missing compatibility manifest");
                COMPATIBILITY.load(in);
            }
            // Verify bytes on the launch classpath, including any loose overrides.
            for (String resource : COMPATIBILITY.stringPropertyNames()) {
                try (InputStream in = ClassLoader.getSystemResourceAsStream(resource)) {
                    if (in == null || !java.util.Arrays.asList(COMPATIBILITY.getProperty(resource).split(",")).contains(hash(in.readAllBytes())))
                        throw new IllegalStateException("unrecognized game class: " + resource);
                }
            }
            instrumentation.addTransformer(new Agent(), false);
            System.out.println("[DropKickLoader] compatibility checks passed; waiting for Lua init");
        } catch (Throwable failure) {
            disable(failure);
        }
    }

    private static String hash(byte[] bytes) throws Exception {
        return HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(bytes));
    }

    @Override
    public byte[] transform(ClassLoader loader, String name, Class<?> redefined,
                            ProtectionDomain domain, byte[] bytes) {
        if (disabled || !TARGET.equals(name) || redefined != null) return null;
        try {
            if (loader != ClassLoader.getSystemClassLoader())
                throw new IllegalStateException("unsupported game classloader");
            if (!hash(bytes).equals(COMPATIBILITY.getProperty(TARGET + ".class")))
                throw new IllegalStateException("LuaManager already changed by another agent");
            return instrument(bytes);
        } catch (Throwable failure) {
            disable(failure);
            return null; // Leave original class untouched on failure.
        }
    }

    public static byte[] instrument(byte[] bytes) {
        ClassFile cf = ClassFile.of();
        int[] exits = {0};
        byte[] result = cf.transformClass(cf.parse(bytes), ClassTransform.transformingMethodBodies(
            method -> method.methodName().equalsString("init")
                    && method.methodType().equalsString("()V"),
            (builder, element) -> {
                if (element instanceof ReturnInstruction ret && ret.opcode() == Opcode.RETURN) {
                    builder.invokestatic(ClassDesc.of("wakaka.dropkick.Agent"), "onLuaReady",
                        MethodTypeDesc.ofDescriptor("()V"));
                    exits[0]++;
                }
                builder.with(element);
            }));
        if (exits[0] != 1) throw new IllegalStateException("unexpected Lua init return count: " + exits[0]);
        return result;
    }

    public static void onLuaReady() {
        if (disabled) return;
        try {
            // Delay game-class linkage until the Lua initialization has finished.
            Class.forName("wakaka.dropkick.Bootstrap").getMethod("install").invoke(null);
        } catch (Throwable failure) {
            disable(failure);
        }
    }

    private static void disable(Throwable failure) {
        disabled = true;
        System.err.println("[DropKickLoader] DISABLED: " + failure);
    }
}
