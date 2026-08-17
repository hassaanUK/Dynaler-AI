package com.dynalerai.app

import android.content.Context
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.util.concurrent.TimeUnit

object ApiClient {
    private val JSON_MEDIA = "application/json; charset=utf-8".toMediaType()
    private val http = OkHttpClient.Builder()
        .connectTimeout(5, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .writeTimeout(5, TimeUnit.SECONDS)
        .build()

    private fun get(url: String): String {
        val req = Request.Builder().url(url).get().build()
        return http.newCall(req).execute().use { it.body?.string() ?: "" }
    }

    private fun post(url: String, json: String = "{}"): String {
        val body = json.toRequestBody(JSON_MEDIA)
        val req = Request.Builder().url(url).post(body).build()
        return http.newCall(req).execute().use { it.body?.string() ?: "" }
    }

    fun status(ctx: Context): String = get("${Prefs.baseUrl(ctx)}/status")

    fun start(ctx: Context, goal: String, mode: Int, model: String,
              apiKey: String, safeMode: Boolean, screenVision: Boolean,
              autoRetry: Boolean): String {
        val safeKey = apiKey.replace("\"", "\\\"")
        val safeGoal = goal.replace("\"", "\\\"")
        val json = """{
  "goal": "$safeGoal",
  "mode": $mode,
  "model": "$model",
  "api_key": "$safeKey",
  "safe_mode": $safeMode,
  "screen_vision": $screenVision,
  "auto_retry": $autoRetry
}"""
        return post("${Prefs.baseUrl(ctx)}/start", json)
    }

    fun stop(ctx: Context): String = post("${Prefs.baseUrl(ctx)}/stop")
    fun log(ctx: Context): String = get("${Prefs.baseUrl(ctx)}/log")
    fun config(ctx: Context): String = get("${Prefs.baseUrl(ctx)}/config")
    fun saveConfig(ctx: Context, json: String): String = post("${Prefs.baseUrl(ctx)}/config", json)
}
