package com.jetbrains.rider.settings

import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg.CgLanguage


class CgCodeStyleSettingsProvider : RiderCodeStyleSettingsProvider() {
    override fun getLanguage() = CgLanguage
    override fun getHelpTopic() = "Settings_Code_Style_CG"
    override fun getConfigurableDisplayName() = CgLanguage.displayName
    override fun getPagesId() = mapOf("CgCodeStyle" to "Formatting Style")
}