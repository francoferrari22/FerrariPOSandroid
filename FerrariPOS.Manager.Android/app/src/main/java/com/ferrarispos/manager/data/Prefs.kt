package com.ferrarispos.manager.data

import android.content.Context
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.first

private val Context.dataStore by preferencesDataStore("ferrari_manager")
class Prefs(private val context: Context) {
    private val urlKey = stringPreferencesKey("base_url")
    private val tokenKey = stringPreferencesKey("token")
    private val themeKey = stringPreferencesKey("theme_name")
    private val soundsKey = booleanPreferencesKey("interface_sounds")
    suspend fun save(baseUrl: String, token: String) { context.dataStore.edit { it[urlKey] = baseUrl.ensureSlash(); it[tokenKey] = token } }
    suspend fun clear() { context.dataStore.edit { it.clear() } }
    suspend fun url(): String = context.dataStore.data.first()[urlKey].orEmpty()
    suspend fun token(): String = context.dataStore.data.first()[tokenKey].orEmpty()
    suspend fun paired(): Boolean = url().isNotBlank() && token().isNotBlank()
    suspend fun theme(): String = context.dataStore.data.first()[themeKey] ?: "Dark Pro"
    suspend fun sounds(): Boolean = context.dataStore.data.first()[soundsKey] ?: true
    suspend fun saveTheme(value:String) { context.dataStore.edit { it[themeKey]=value } }
    suspend fun saveSounds(value:Boolean) { context.dataStore.edit { it[soundsKey]=value } }
}
private fun String.ensureSlash() = if (endsWith("/")) this else "$this/"
