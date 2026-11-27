# Daily Caravan 0.1.5 Verification

Thunderstore 0.1.4 contains a different DLL from the approved Test - Mods candidate. The controller-approved gameplay build therefore ships under 0.1.5.

| Artifact | SHA-256 |
| --- | --- |
| Approved 0.1.4 ZIP | `812cfba818005166d654eee3b11172ce5cffd20340fa6fa3e16bdfce69448bc7` |
| Approved 0.1.4 DLL | `3bf6e108de16fa7fab5d1e3506a0ef9fea1985fdf0df755e36423a70fb649e88` |
| Published 0.1.4 DLL | `e993f5dc2402dc1c8def929312f9f736f2a0195fc8d4f6f514bd689ed972645e` |
| Prepared 0.1.5 ZIP | `887699b102fa1570d7334cf0da351d482aa07d0597fe855eabf5767149889ee8` |
| Prepared 0.1.5 DLL | `4ff59bd5dc5ad1ad3ebc72db97bb58da61efdf1d5ed39a7031c7599c7bee0d0c` |

The approved source rebuilt with identical IL at 0.1.4. Comparing the approved DLL directly with the 0.1.5 Release build showed the same IL across all 142 methods and the same emitted type definitions, apart from the BepInPlugin version value. Mono.Cecil verified identical non-version assembly metadata, references, module attributes, and embedded-resource inventory and hashes. The assembly, file, and informational versions changed, and the informational version records the new source commit.

The 43 schedule checks and local package verification passed. The release uses the approved 256 × 256 icon. Gameplay evidence remains attributed to the approved 0.1.4 candidate on game 0.8.070. Virtual input verified right-click reopen and controller focus restoration in Human and Undead at both UI scales. The user separately approved the combined profile after physical controller testing.

- [Human caravan at 1×](../screenshots/current/human-caravan-1x.png)
- [Undead caravan at 1.5×](../screenshots/current/undead-caravan-1p5x.png)
