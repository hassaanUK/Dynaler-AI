package com.dynalerai.app

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.dynalerai.app.databinding.ActivityLogBinding
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class LogActivity : AppCompatActivity() {
    private lateinit var binding: ActivityLogBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityLogBinding.inflate(layoutInflater)
        setContentView(binding.root)
        setSupportActionBar(binding.toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.title = "Activity Log"
        binding.btnRefresh.setOnClickListener { loadLog() }
        loadLog()
    }

    private fun loadLog() {
        binding.progressBar.visibility = View.VISIBLE
        binding.tvLog.text = ""
        lifecycleScope.launch {
            try {
                val text = withContext(Dispatchers.IO) { ApiClient.log(this@LogActivity) }
                binding.tvLog.text = text.ifBlank { "(No log entries yet)" }
                binding.scrollView.post { binding.scrollView.fullScroll(View.FOCUS_DOWN) }
            } catch (e: Exception) {
                Toast.makeText(this@LogActivity, "Error: ${e.message}", Toast.LENGTH_SHORT).show()
                binding.tvLog.text = "Error: ${e.message}"
            } finally { binding.progressBar.visibility = View.GONE }
        }
    }

    override fun onSupportNavigateUp(): Boolean { onBackPressedDispatcher.onBackPressed(); return true }
}
