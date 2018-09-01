/*
 * Certain versions of software and/or documents ("Material") accessible here may contain branding from
 * Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.  As of September 1, 2017,
 * the Material is now offered by Micro Focus, a separately owned and operated company.  Any reference to the HP
 * and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE
 * marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * (c) Copyright 2012-2018 Micro Focus or one of its affiliates.
 *
 * The only warranties for products and services of Micro Focus and its affiliates
 * and licensors ("Micro Focus") are set forth in the express warranty statements
 * accompanying such products and services. Nothing herein should be construed as
 * constituting an additional warranty. Micro Focus shall not be liable for technical
 * or editorial errors or omissions contained herein.
 * The information contained herein is subject to change without notice.
 * ___________________________________________________________________
 */

package com.microfocus.application.automation.tools.common;

import hudson.AbortException;

import javax.annotation.Nonnull;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.concurrent.TimeUnit;
import java.util.stream.Stream;

public class HealthAnalyzerCommon {
    private HealthAnalyzerCommon() {
    }

    public static void ifCheckedPerformWindowsInstallationCheck
            (@Nonnull final String registryPath, @Nonnull final boolean toCheck, @Nonnull final String productName)
            throws IOException, InterruptedException {
        if (toCheck && !HealthAnalyzerCommon.isRegistryExists(registryPath))
            throw new AbortException(String.format("%s is not installed, please install it first", productName));
    }

    private static boolean isRegistryExists(@Nonnull final String registryPath) throws IOException, InterruptedException {
        // TODO: Check if its windows? (System.getProperty("os.name")
        return startRegistryQueryAndGetStatus(registryPath);
    }

    private static boolean startRegistryQueryAndGetStatus(@Nonnull final String registryPath)
            throws IOException, InterruptedException {
        ProcessBuilder builder = new ProcessBuilder("reg", "query", registryPath);
        Process reg = builder.start();
        boolean keys = isProcessStreamHasRegistry(reg);
        reg.waitFor(1, TimeUnit.MINUTES);
        return keys;
    }

    private static boolean isProcessStreamHasRegistry(@Nonnull final Process reg) throws IOException {
        try (BufferedReader output = new BufferedReader(
                new InputStreamReader(reg.getInputStream()))) {
            Stream<String> keys = output.lines().filter(l -> !l.isEmpty());
            return keys.findFirst().isPresent();
        }
    }
}
