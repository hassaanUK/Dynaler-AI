package com.dynalerai.app

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.LiveData
import androidx.lifecycle.MutableLiveData
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject

class MainViewModel(app: Application) : AndroidViewModel(app) {

    private val _status  = MutableLiveData("Idle")
    val status: LiveData<String> get() = _status

    private val _running = MutableLiveData(false)
    val running: LiveData<Boolean> get() = _running

    private val _plan    = MutableLiveData("—")
    val plan: LiveData<String> get() = _plan

    private val _error   = MutableLiveData<String?>(null)
    val error: LiveData<String?> get() = _error

    private var pollJob: Job? = null

    fun startPolling() {
        pollJob?.cancel()
        pollJob = viewModelScope.launch {
            while (isActive) { fetchStatus(); delay(2000) }
        }
    }

    fun stopPolling() { pollJob?.cancel() }

    private suspend fun fetchStatus() {
        try {
            val raw = withContext(Dispatchers.IO) { ApiClient.status(getApplication()) }
            val obj = JSONObject(raw)
            _status.postValue(obj.optString("status", "Unknown"))
            _running.postValue(obj.optBoolean("running", false))
            val planText = obj.optString("plan", "")
            if (planText.isNotBlank()) _plan.postValue(planText)
            _error.postValue(null)
        } catch (e: Exception) {
            _status.postValue("Offline")
            _running.postValue(false)
            _error.postValue("Cannot reach PC: ${e.message}")
        }
    }

    fun sendStart(goal: String, mode: Int, model: String, apiKey: String,
                  safeMode: Boolean, screenVision: Boolean, autoRetry: Boolean,
                  onResult: (Boolean, String) -> Unit) {
        viewModelScope.launch {
            try {
                val raw = withContext(Dispatchers.IO) {
                    ApiClient.start(getApplication(), goal, mode, model, apiKey, safeMode, screenVision, autoRetry)
                }
                val ok = raw.contains("\"ok\"") || raw.contains("\"started\"")
                onResult(ok, raw)
            } catch (e: Exception) { onResult(false, e.message ?: "Error") }
        }
    }

    fun sendStop(onResult: (Boolean) -> Unit) {
        viewModelScope.launch {
            try { withContext(Dispatchers.IO) { ApiClient.stop(getApplication()) }; onResult(true) }
            catch (e: Exception) { onResult(false) }
        }
    }
}
