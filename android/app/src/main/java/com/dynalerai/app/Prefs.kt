package com.dynalerai.app

import android.content.Context

object Prefs {
    private const val FILE = "dynaler_prefs"
    private const val KEY_HOST = "host"
    private const val KEY_PORT = "port"
    private const val DEFAULT_PORT = "5757"

    fun getHost(ctx: Context): String =
        ctx.getSharedPreferences(FILE, Context.MODE_PRIVATE).getString(KEY_HOST, "") ?: ""

    fun getPort(ctx: Context): String =
        ctx.getSharedPreferences(FILE, Context.MODE_PRIVATE).getString(KEY_PORT, DEFAULT_PORT) ?: DEFAULT_PORT

    fun save(ctx: Context, host: String, port: String) {
        ctx.getSharedPreferences(FILE, Context.MODE_PRIVATE).edit()
            .putString(KEY_HOST, host.trim())
            .putString(KEY_PORT, port.trim().ifEmpty { DEFAULT_PORT })
            .apply()
    }

    fun baseUrl(ctx: Context): String {
        val h = getHost(ctx).trimEnd('/')
        val p = getPort(ctx)
        return "http://$h:$p"
    }
}
