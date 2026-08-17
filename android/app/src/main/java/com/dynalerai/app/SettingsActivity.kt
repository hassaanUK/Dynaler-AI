package com.dynalerai.app

import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.dynalerai.app.databinding.ActivitySettingsBinding

class SettingsActivity : AppCompatActivity() {
    private lateinit var binding: ActivitySettingsBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivitySettingsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        setSupportActionBar(binding.toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        supportActionBar?.title = "Settings"
        binding.etHost.setText(Prefs.getHost(this))
        binding.etPort.setText(Prefs.getPort(this))
        binding.btnSave.setOnClickListener {
            val host = binding.etHost.text.toString().trim()
            if (host.isEmpty()) { Toast.makeText(this, "Enter the PC IP address", Toast.LENGTH_SHORT).show(); return@setOnClickListener }
            Prefs.save(this, host, binding.etPort.text.toString())
            Toast.makeText(this, "Saved!", Toast.LENGTH_SHORT).show()
            finish()
        }
    }

    override fun onSupportNavigateUp(): Boolean { onBackPressedDispatcher.onBackPressed(); return true }
}
