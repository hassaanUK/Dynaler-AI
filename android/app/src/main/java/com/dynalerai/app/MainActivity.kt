package com.dynalerai.app

import android.content.Intent
import android.os.Bundle
import android.view.Menu
import android.view.MenuItem
import android.view.View
import android.widget.ArrayAdapter
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.ViewModelProvider
import com.dynalerai.app.databinding.ActivityMainBinding

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private lateinit var vm: MainViewModel

    private val modelOptions = mapOf(
        0 to listOf("gpt-4o", "gpt-4o-mini", "gpt-4-turbo"),
        1 to listOf("gpt-4o", "gpt-4o-mini", "gpt-4-turbo"),
        2 to listOf("claude-3-5-sonnet-20241022", "claude-3-haiku-20240307"),
        3 to listOf("gemini-1.5-pro", "gemini-1.5-flash")
    )

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        setSupportActionBar(binding.toolbar)
        vm = ViewModelProvider(this)[MainViewModel::class.java]
        setupModeSpinner()
        observeViewModel()
        binding.btnStart.setOnClickListener { doStart() }
        binding.btnStop.setOnClickListener  { doStop()  }
    }

    override fun onResume() {
        super.onResume()
        updateConnectionLabel()
        vm.startPolling()
    }

    override fun onPause() {
        super.onPause()
        vm.stopPolling()
    }

    private fun setupModeSpinner() {
        val modes = listOf("Built-in AI (ChatGPT)", "Custom OpenAI", "Anthropic Claude", "Google Gemini")
        binding.spinnerMode.adapter = ArrayAdapter(this, android.R.layout.simple_spinner_dropdown_item, modes)
        binding.spinnerMode.setOnItemSelectedListener(object : android.widget.AdapterView.OnItemSelectedListener {
            override fun onItemSelected(p: android.widget.AdapterView<*>?, v: View?, pos: Int, id: Long) {
                binding.spinnerModel.adapter = ArrayAdapter(this@MainActivity,
                    android.R.layout.simple_spinner_dropdown_item, modelOptions[pos] ?: listOf("gpt-4o"))
                binding.layoutApiKey.visibility = if (pos > 0) View.VISIBLE else View.GONE
            }
            override fun onNothingSelected(p: android.widget.AdapterView<*>?) {}
        })
        binding.spinnerModel.adapter = ArrayAdapter(this, android.R.layout.simple_spinner_dropdown_item, modelOptions[0]!!)
        binding.layoutApiKey.visibility = View.GONE
    }

    private fun observeViewModel() {
        vm.status.observe(this) { s ->
            binding.tvStatus.text = s
            binding.tvStatus.setTextColor(when (s.lowercase()) {
                "running" -> getColor(R.color.green)
                "offline" -> getColor(R.color.red)
                else      -> getColor(R.color.purple_200)
            })
        }
        vm.running.observe(this) { r -> binding.btnStart.isEnabled = !r; binding.btnStop.isEnabled = r }
        vm.plan.observe(this)   { binding.tvPlan.text = it }
        vm.error.observe(this)  { err -> binding.tvError.text = err ?: ""; binding.tvError.visibility = if (err != null) View.VISIBLE else View.GONE }
    }

    private fun doStart() {
        val goal = binding.etGoal.text.toString().trim()
        if (goal.isEmpty()) { toast("Enter a goal first"); return }
        val mode   = binding.spinnerMode.selectedItemPosition
        val model  = binding.spinnerModel.selectedItem?.toString() ?: "gpt-4o"
        val apiKey = binding.etApiKey.text.toString().trim()
        binding.btnStart.isEnabled = false
        vm.sendStart(goal, mode, model, apiKey,
            binding.switchSafeMode.isChecked, binding.switchScreenVision.isChecked, binding.switchAutoRetry.isChecked
        ) { ok, msg -> runOnUiThread { if (ok) toast("AI started!") else toast("Error: $msg") } }
    }

    private fun doStop() { vm.sendStop { ok -> runOnUiThread { toast(if (ok) "Stopped" else "Stop failed") } } }

    override fun onCreateOptionsMenu(menu: Menu): Boolean { menuInflater.inflate(R.menu.main_menu, menu); return true }
    override fun onOptionsItemSelected(item: MenuItem) = when (item.itemId) {
        R.id.menu_settings -> { startActivity(Intent(this, SettingsActivity::class.java)); true }
        R.id.menu_log      -> { startActivity(Intent(this, LogActivity::class.java)); true }
        else               -> super.onOptionsItemSelected(item)
    }

    private fun updateConnectionLabel() {
        val host = Prefs.getHost(this)
        binding.tvConnection.text = if (host.isBlank()) "Not configured — open Settings" else "PC: ${Prefs.baseUrl(this)}"
    }

    private fun toast(msg: String) = Toast.makeText(this, msg, Toast.LENGTH_SHORT).show()
}
