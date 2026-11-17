"""Sons da v5 (#523): monta cada efeito e ambiente a partir de gravações (Gamemaster Pro Sound Collection, do Pietro,
e Sonniss GDC 2026), grava em Assets/Audio/V5 e troca o conteúdo dos sons sintetizados da energia (mesmo arquivo, mesmo
.meta). A música, as vinhetas e o arpejo de conclusão saem do musica_v5.py; o banco (volumes, variação, grupo) do
banco_v5.py.

  C:\\Users\\PIETRO\\analises-lilith\\.venv-video\\Scripts\\python.exe -X utf8 Tools\\Audio\\sons_v5.py [id ...]
"""
import json
import sys

import numpy as np

from audio_kit import (ASSETS, RAIZ, SR, aparar_cauda, celular, credito, cortar_antes_do_pico, estereo, fade,
                       fontes_usadas, gm, gravar, integrado, laco, misturar, mono, nivelar, passa_alta, passa_baixa,
                       passa_banda, presenca, realce, reverb, seg, son, tom)

V5 = ASSETS / 'Audio' / 'V5'
ENERGIA = ASSETS / 'Systems' / 'Energy' / 'Resources' / 'FireEnergy'
B2 = 'Sonniss.com-GDC2026-GameAudioBundle2of5/'
SON_UI = B2 + 'Cinematic Sound Design - UI Interaction Elements/'
SON_INFO = B2 + 'Cinematic Sound Design - Interface & Infographics/'
SON_FEED = B2 + 'Cinematic Sound Design - System & UI Feedback Elements/'
SON_PAPEL = B2 + 'Cinematic Sound Design - Paper Foley/'


def G(rel, est=False, som=None):
    """Gravação da Gamemaster, anotada nos créditos do som que está sendo montado."""
    credito(som or ATUAL[0], 'Gamemaster Pro Sound Collection: ' + rel)
    return gm(rel, est)


def S(rel, est=False, som=None):
    credito(som or ATUAL[0], 'Sonniss GDC 2026: ' + rel.split('/', 1)[1])
    return son(rel, est)


ATUAL = ['']
RECEITAS = {}


def receita(pasta, alvo, teto=-1.0, ogg=False, integrado_alvo=None, destino=None):
    """Registra uma receita: a função devolve uma lista de variações; cada uma vira <id>_NN."""
    def marcar(f):
        RECEITAS[f.__name__] = dict(f=f, pasta=pasta, alvo=alvo, teto=teto, ogg=ogg, integrado=integrado_alvo,
                                    destino=destino)
        return f
    return marcar


def curto(x, dur, saida=0.04):
    return fade(x[:int(dur * SR)], saida=saida)


def blip(st=0.0):
    """Bipe curto da Gamemaster (beep_08, 0,06 s) no tom pedido."""
    return tom(aparar_cauda(G('Alarms_Beeps_Siren/beep_08.wav')), st)


def em_volta_do_pico(x, antes, depois):
    k = int(np.abs(mono(x)).argmax())
    return x[max(0, k - int(antes * SR)):k + int(depois * SR)]


def trecho_estavel(x, dur, passo=1.0):
    """Janela de `dur` s com o nível mais parelho (menor desvio do RMS por segundo): fundo sem buzina nem grito."""
    n = int(passo * SR)
    nv = np.array([10 * np.log10(np.mean(mono(x[i:i + n]) ** 2) + 1e-12) for i in range(0, len(x) - n, n)])
    k = int(dur / passo)
    melhor = min(range(len(nv) - k), key=lambda i: np.std(nv[i:i + k]) + 0.3 * (nv[i:i + k].max() - nv[i:i + k].mean()))
    return x[melhor * n:melhor * n + int(dur * SR)]


# ---------------------------------------------------------------- INTERFACE

@receita('UI', -20, teto=-3)
def ui_click():
    return [aparar_cauda(curto(G(f'User_Interface_Menu/ui_button_simple_click_0{i}.wav'), 0.3)) for i in (3, 4)]


@receita('UI', -21, teto=-3)
def ui_tab():
    papel = passa_alta(curto(S(SON_PAPEL + 'Encyclopedia Glossy Page Turn Muted.wav'), 0.32, 0.12), 250)
    return [aparar_cauda(G('User_Interface_Menu/ui_menu_button_scroll_page_03.wav')),
            aparar_cauda(G('User_Interface_Menu/ui_menu_button_scroll_page_01.wav')),
            misturar((aparar_cauda(G('User_Interface_Menu/ui_menu_button_scroll_page_03.wav')), 0, -4),
                     (cortar_antes_do_pico(papel, 0.004), 0.0, 0))]


@receita('UI', -19, teto=-3)
def ui_popup():
    return [aparar_cauda(G('User_Interface_Menu/ui_menu_popup_message_01.wav')),
            aparar_cauda(S(SON_INFO + 'Interface Pop High Short.wav'))]


@receita('UI', -15, teto=-1.5)
def ui_buy():
    caixa = G('Collectibles_Items_Powerup/cash_register_open_coins_cha_ching_01.wav')
    return [aparar_cauda(misturar((caixa, 0, 0),
                                  (cortar_antes_do_pico(S(SON_UI + 'Ting Coins.wav')), 0.05, -5),
                                  (cortar_antes_do_pico(S(SON_INFO + 'Interface Accept Glassy Snap.wav')), 0.0, -9)))]


@receita('UI', -18, teto=-3)
def ui_denied():
    return [aparar_cauda(S(SON_UI + 'Deny Muted.wav')),
            aparar_cauda(passa_alta(S(SON_FEED + 'Interface Deny Low Fat Dark.wav'), 140))]


@receita('UI', -21, teto=-3)
def ui_coin_tick():
    return [curto(cortar_antes_do_pico(G(f'Collectibles_Items_Powerup/collect_coin_0{i}.wav'), 0.002), 0.17, 0.08)
            for i in (3, 4)]


# ---------------------------------------------------------------- CHAMADOS E EQUIPE

@receita('Chamados', -16)
def call_new_fire():
    chama = passa_alta(G('Explosion_Fire_Gas/gas_small_flame_ignite_01.wav'), 400)
    faisca = G('Explosion_Fire_Gas/fire_making_grind_stone_flint_spark_01.wav')
    return [misturar((blip(0), 0.0, 0), (blip(5), 0.085, -1), (blip(10), 0.17, -2), (chama, 0.0, -5), (faisca, 0.04, -9)),
            misturar((blip(2), 0.0, 0), (blip(7), 0.085, -1), (blip(12), 0.17, -2), (chama, 0.02, -6))]


@receita('Chamados', -16)
def call_new_storm():
    radio = passa_banda(G('Electricity_Hums/radio_walkie_talkie_static_01.wav'), 400, 4000)
    gota = G('Water/water_drop_drip_single_01.wav')
    return [misturar((radio, 0.0, -7), (blip(3), 0.1, 0), (blip(-2), 0.2, -1), (gota, 0.31, -4)),
            misturar((radio, 0.0, -8), (blip(5), 0.1, 0), (blip(0), 0.2, -1), (gota, 0.3, -5))]


@receita('Chamados', -16)
def call_new_quake():
    pedra = passa_alta(G('Impacts_Smashable/rock_impact_small_hit_01.wav'), 220)
    return [misturar((pedra, 0.0, -3), (blip(-5), 0.07, 0), (blip(-5), 0.19, -1)),
            misturar((passa_alta(G('Impacts_Smashable/rock_impact_small_hit_03.wav'), 220), 0.0, -3), (blip(-3), 0.07, 0),
                     (blip(-3), 0.19, -1))]


@receita('Chamados', -16)
def call_new_power():
    zum = fade(passa_alta(seg(G('Electricity_Hums/hum_electric_sparks_interference_loop_01.wav'), 0.2, 0.55), 300), 0.02, 0.12)
    return [misturar((G('Switches_Buttons_Gears_Levers/switch_button_push_on_off_02.wav'), 0.0, -2), (blip(7), 0.06, 0),
                     (blip(7), 0.16, -2), (zum, 0.02, -9))]


@receita('Chamados', -17)
def call_urgent():
    radio = passa_banda(G('Electricity_Hums/radio_walkie_talkie_static_02.wav'), 500, 4500)
    return [misturar((blip(12), 0.0, 0), (blip(12), 0.075, -1), (radio, 0.14, -12))]


@receita('Chamados', -19)
def team_depart():
    saida = []
    for i in range(1, 5):
        tralha = cortar_antes_do_pico(G(f'Foley/foley_soldier_gear_equipment_rattle_movement_med_0{i}.wav'), 0.02)
        passos = [(G(f'Footsteps/footstep_concrete_run_0{(i + k) % 4 + 1}.wav'), 0.05 + 0.15 * k, -2 - 3 * k) for k in range(4)]
        saida.append(aparar_cauda(misturar((tralha, 0, 0), *passos)))
    return saida


@receita('Chamados', -20)
def team_arrive():
    return [aparar_cauda(misturar((G(f'Foley/foley_sports_bag_grab_pickup_catch_0{i}.wav'), 0, 0),
                                  (G(f'Footsteps/footstep_concrete_land_0{i - 1}.wav'), 0, -4))) for i in (2, 3)]


@receita('Chamados', -21)
def repair_tool():
    """Ferramentas do conserto: martelo (madeira + prego), catraca e furadeira; o AudioManager sorteia no laço."""
    saida = []
    for madeira, metal in ((1, 2), (3, 5), (4, 2)):
        prego = curto(G(f'Metal/metal_hit_small_0{metal}.wav'), 0.14)
        saida.append(aparar_cauda(misturar((G(f'Miscellaneous/Wood/wood_block_sticks_hit_clap_0{madeira}.wav'), 0, 0),
                                           (prego, 0.002, -11))))
    saida += [aparar_cauda(G(f'Switches_Buttons_Gears_Levers/lever_turn_push_crank_handle_small_0{i}.wav')) for i in (2, 4)]
    saida.append(aparar_cauda(G('Switches_Buttons_Gears_Levers/gear_drill_turn_04.wav')))
    return saida


@receita('Chamados', -15, teto=-1.5)
def call_collect():
    moeda = G('Collectibles_Items_Powerup/collect_coin_05.wav')
    ting = cortar_antes_do_pico(S(SON_UI + 'Ting Coins.wav'))
    saco = G('Collectibles_Items_Powerup/coin_bag_ring_gemstone_item_01.wav')
    return [aparar_cauda(misturar((moeda, 0, 0), (ting, 0.03, -4), (saco, 0.06, -10)))]


# ---------------------------------------------------------------- FOGO

@receita('Desastres', -16)
def fire_ignite():
    saida = []
    for i in (1, 2):
        chama = realce(passa_alta(G(f'Explosion_Fire_Gas/gas_med_flame_ignite_0{i}.wav'), 60), 1500, 5)
        faisca = G(f'Explosion_Fire_Gas/fire_making_grind_stone_flint_spark_0{i}.wav')
        estalo = seg(G('Explosion_Fire_Gas/fire_burning_flames_crackle_loop_01.wav'), 1.0, 1.9)
        saida.append(aparar_cauda(misturar((chama, 0, 0), (faisca, 0.02, -6), (fade(estalo, 0.05, 0.4), 0.08, -8))))
    return saida


@receita('Desastres', None, integrado_alvo=-21)
def fire_jet_loop():
    chuveiro = passa_banda(seg(G('Water/water_running_shower_dripping_loop_02.wav'), 2.0, 6.2), 450, 9000)
    corpo = passa_baixa(seg(G('Water/water_running_shower_dripping_loop_04.wav'), 1.0, 5.2), 900)
    chiado = passa_alta(np.tile(G('Explosion_Fire_Gas/gas_leak_03_loop.wav'), 3)[:len(chuveiro)], 1800)
    return [laco(misturar((chuveiro, 0, 0), (corpo, 0, -4), (chiado, 0, -10)), 0.5)]


@receita('Desastres', -17)
def fire_jet_start():
    jato = G('Explosion_Fire_Gas/gas_leak_short_burst_01.wav')
    return [aparar_cauda(misturar((jato, 0, 0), (passa_alta(jato, 1800), 0, -4),
                                  (G('Water/water_splash_small_item_01.wav'), 0.12, -2)))]


@receita('Desastres', -16)
def fire_out():
    vapor = passa_baixa(G('Explosion_Fire_Gas/gas_leak_med_burst_01.wav'), 6500)
    return [aparar_cauda(misturar((G('Explosion_Fire_Gas/fuse_stop_burn_out_bomb_dynamite_01.wav'), 0, 0),
                                  (fade(vapor, 0.05, 0.5), 0.0, -5), (G('Water/water_splash_small_item_02.wav'), 0.02, -9)))]


@receita('Desastres', -17)
def warning_fire():
    estalo = fade(seg(G('Explosion_Fire_Gas/fire_burning_flames_crackle_loop_01.wav', True), 0.0, 2.2), 1.0, 0.6)
    chama = estereo(realce(passa_alta(G('Explosion_Fire_Gas/gas_large_flame_ignite_01.wav'), 60), 1200, 4))
    return [misturar((estalo, 0, 0), (chama, 0.85, -2), estereo_saida=True)]


# ---------------------------------------------------------------- TEMPESTADE

@receita('Ambiente', None, ogg=True, integrado_alvo=-27)
def rain_light_loop():
    return [laco(G('Animals_Nature_Ambiences/rain_light_hard_surface_loop_01.wav', True), 0.8)]


@receita('Ambiente', None, ogg=True, integrado_alvo=-23)
def rain_heavy_loop():
    chuva = S('Sonniss.com-GDC2026-GameAudioBundle5of5/The Noisery - City Rain/'
              'RAINConc_Rain Medium Exterior Splatter City Traffic 6_The Noisery_City Rain.wav', True)
    return [laco(trecho_estavel(chuva, 13.0), 1.0)]


@receita('Ambiente', None, ogg=True, integrado_alvo=-28)
def wind_loop():
    return [laco(seg(G('Animals_Nature_Ambiences/wind_general_gusty_low_loop_02.wav', True), 0.0, 18.0), 2.0)]


@receita('Desastres', -11)
def thunder_near():
    """O estalo fica no começo do arquivo (10 ms antes do pico): soa no mesmo quadro do raio."""
    estalo = passa_alta(seg(G('Magic_Spells/electric_lightning_blast_01.wav'), 0, 0.5), 1500)
    texas = S(B2 + 'Epic Stock Media - Public Spaces - Storms Lakes Parks and Rural Nature Exteriors/'
              'STORM_Texas Rain Thunder Initial Crash Boom Storm 01 Clap Lightning_ESM_CPS.wav')
    saida = []
    for corpo in (G('Animals_Nature_Ambiences/thunder_lightning_strike_rumble_01.wav'),
                  G('Animals_Nature_Ambiences/thunder_lightning_strike_rumble_02.wav'),
                  em_volta_do_pico(texas, 0.3, 6.0)):
        corpo = realce(cortar_antes_do_pico(corpo, 0.01, -6), 900, 3)
        saida.append(fade(misturar((corpo[:int(5.5 * SR)], 0, 0), (fade(estalo, 0, 0.3), 0.0, -9)), saida=1.2))
    return saida


@receita('Desastres', -21)
def thunder_far():
    saida = []
    for i in (3, 5):
        x = realce(passa_baixa(G(f'Animals_Nature_Ambiences/thunder_lightning_strike_rumble_0{i}.wav'), 2800), 320, 6)
        saida.append(fade(x[:int(6 * SR)], 0.25, 1.5))
    return saida


@receita('Desastres', -17)
def warning_storm():
    vento = fade(seg(G('Animals_Nature_Ambiences/wind_general_gusty_low_loop_02.wav', True), 17.0, 21.0), 1.5, 1.5)
    trovao = estereo(fade(realce(passa_baixa(G('Animals_Nature_Ambiences/thunder_lightning_strike_rumble_04.wav'), 3000),
                                 300, 5)[:int(3.5 * SR)], 0.2, 1.2))
    return [misturar((vento, 0, 0), (trovao, 0.9, -3), estereo_saida=True)]


# ---------------------------------------------------------------- TERREMOTO

@receita('Desastres', -12)
def quake_rumble():
    camadas = misturar(
        (G('Impacts_Smashable/rock_earthquake_impact_01.wav'), 0, 0),
        (G('Cinematic Sounds/cinematic_deep_bass_rumble_02.wav'), 0, -5),
        (realce(G('Impacts_Smashable/rock_avalanche_landslide_debris_03.wav'), 700, 4), 0.25, -3),
        (G('Impacts_Smashable/rock_smashable_falling_debris_04.wav'), 0.9, -2),
        (G('Impacts_Smashable/glass_smashable_debris_fall_01.wav'), 1.5, -12))
    return [fade(camadas[:int(5.2 * SR)], saida=1.4)]


@receita('Desastres', -17)
def quake_hit():
    vidro = G('Impacts_Smashable/glass_smashable_small_break_01.wav')
    saida = []
    for i in (1, 2, 3):
        pedra = curto(G(f'Impacts_Smashable/rock_smashable_hit_impact_0{i}.wav'), 1.3, 0.4)
        saida.append(aparar_cauda(misturar((pedra, 0, 0), (vidro, 0.02, -14 if i == 1 else -40))))
    return saida


@receita('Desastres', -15)
def warning_quake():
    sopro = seg(G('Cinematic Sounds/cinematic_deep_bass_pass_whoosh_01.wav'), 0, 3.6)
    batida = realce(G('Impacts_Smashable/rock_impact_heavy_slam_02.wav'), 600, 6)
    # louça e móveis tremendo antes da batida e cascalho depois: o aviso aparece no alto-falante pequeno
    chacoalho = fade(G('Miscellaneous/Wood/wood_block_rattle_movement_02.wav'), 0.9, 0.3)
    cascalho = passa_alta(G('Miscellaneous/Gravel Dirt Stones/dirt_rice_pouring_gravel_debris_03.wav'), 400)
    return [fade(misturar((realce(sopro, 180, 6), 0, 0), (chacoalho, 0.5, -8), (batida, 1.62, -3),
                          (fade(cascalho, 0.02, 0.6), 1.66, -9)), saida=0.8)]


# ---------------------------------------------------------------- ENERGIA (mesmo arquivo dos sons sintetizados)

def zumbido(ini, fim):
    """Zumbido da rede: o hum de 60 Hz sem o grave que o alto-falante pequeno não toca, com os harmônicos realçados."""
    x = seg(G('Electricity_Hums/hum_electric_neon_light_loop_01.wav'), ini, fim)
    return realce(passa_alta(x, 110), 360, 6)


@receita('Energia', -14, destino='sfx_blackout')
def power_down():
    surto = passa_alta(cortar_antes_do_pico(G('Magic_Spells/electric_surge_blast_02.wav'), 0.005), 300)
    return [aparar_cauda(misturar((G('Switches_Buttons_Gears_Levers/switch_button_push_on_off_04.wav'), 0, 0),
                                  (fade(surto[:int(0.8 * SR)], 0, 0.3), 0.015, -6),
                                  (G('Sci-Fi/sci-fi_power_down_02.wav'), 0.05, -4),
                                  (fade(zumbido(2.0, 2.9), 0, 0.6), 0.0, -7)))]


@receita('Energia', -15, destino='sfx_restore')
def power_restore():
    pisca = realce(passa_alta(G('Miscellaneous/Light/light_fluorescent_hum_flicker_01.wav'), 130), 1000, 6)
    return [fade(misturar((G('Switches_Buttons_Gears_Levers/switch_button_push_on_off_01.wav'), 0, 0),
                          (pisca, 0.07, -2), (fade(zumbido(2.0, 3.8), 0.5, 0.5), 0.3, -8),
                          (G('Sci-Fi/sci-fi_power_up_01.wav'), 0.0, -13)), saida=0.3)]


@receita('Energia', -16, destino='sfx_energy_on')
def energy_unlock():
    pisca = realce(passa_alta(G('Miscellaneous/Light/light_fluorescent_hum_flicker_02.wav'), 130), 1000, 6)
    return [aparar_cauda(misturar((G('Sci-Fi/sci-fi_power_up_01.wav'), 0, -3), (pisca, 0.25, -3),
                                  (G('Switches_Buttons_Gears_Levers/switch_button_push_on_off_03.wav'), 0.2, -4)))]


@receita('Energia', -16, destino='sfx_overload')
def power_overload():
    faisca = fade(passa_alta(seg(G('Electricity_Hums/hum_electric_sparks_interference_loop_01.wav'), 0.0, 0.7), 250), 0.01, 0.2)
    return [aparar_cauda(misturar((faisca, 0, 0), (G('Explosion_Fire_Gas/fire_making_grind_stone_flint_spark_02.wav'), 0.05, -4)))]


@receita('Energia', -18, destino='sfx_no_energy')
def power_denied():
    return [aparar_cauda(misturar((G('Switches_Buttons_Gears_Levers/switch_button_push_small_01.wav'), 0, 0),
                                  (fade(zumbido(3.0, 3.35), 0.01, 0.15), 0.03, -6)))]


@receita('Energia', -16, destino='sfx_fuse_open')
def fuse_open():
    return [aparar_cauda(cortar_antes_do_pico(G('Metal/metal_lid_movement_impact_01.wav'), 0.08, -20))]


@receita('Energia', -18, destino='sfx_fuse_switch')
def fuse_switch():
    return [aparar_cauda(G('Switches_Buttons_Gears_Levers/switch_button_push_on_off_02.wav'))]


@receita('Energia', -17, destino='sfx_fuse_wrong')
def fuse_wrong():
    faisca = fade(passa_alta(seg(G('Electricity_Hums/hum_electric_sparks_interference_loop_01.wav'), 1.0, 1.35), 250), 0.005, 0.1)
    return [aparar_cauda(misturar((faisca, 0, 0), (S(SON_UI + 'Deny Muted.wav'), 0.02, -4)))]


@receita('Energia', -15, destino='sfx_fuse_done')
def fuse_done():
    pisca = realce(passa_alta(G('Miscellaneous/Light/light_fluorescent_hum_flicker_02.wav'), 130), 1000, 6)
    return [aparar_cauda(misturar((G('Switches_Buttons_Gears_Levers/switch_button_push_on_off_04.wav'), 0, 0),
                                  (pisca, 0.06, -3), (G('Sci-Fi/sci-fi_power_up_01.wav'), 0.0, -10)))]


# ---------------------------------------------------------------- PARA-RAIO E OBRAS

@receita('Desastres', -11)
def rod_absorb():
    surto = cortar_antes_do_pico(G('Magic_Spells/electric_surge_blast_01.wav'), 0.008)
    anel = reverb(tom(G('Metal/metal_hit_small_06.wav'), -7), sala=0.5, umido=0.3)
    escudo = G('Sci-Fi/sci-fi_shield_power_deflect_block_01.wav')
    corpo = realce(cortar_antes_do_pico(G('Animals_Nature_Ambiences/thunder_lightning_strike_rumble_02.wav'), 0.01, -6), 900, 3)
    return [fade(misturar((surto, 0, 0), (anel, 0.03, -6), (escudo, 0.01, -9), (corpo[:int(4 * SR)], 0, -5)), saida=1.0)]


@receita('Desastres', -17)
def build_start():
    return [aparar_cauda(misturar((G('User_Interface_Menu/ui_stamp_01.wav'), 0, 0),
                                  (G('Foley/foley_soldier_gear_equipment_rattle_movement_light_01.wav'), 0.08, -8),
                                  (G('Miscellaneous/Wood/wood_block_sticks_hit_clap_02.wav'), 0.26, -9),
                                  (G('Miscellaneous/Wood/wood_block_sticks_hit_clap_05.wav'), 0.44, -11)))]


@receita('Desastres', -15)
def build_done():
    baque = passa_alta(G('Metal/metal_drum_impact_thud_02.wav'), 70)
    brilho = passa_alta(G('Miscellaneous/Bell/chime_bell_02.wav'), 2000)
    return [aparar_cauda(misturar((baque, 0, 0), (G('Impacts_Smashable/rock_impact_small_hit_02.wav'), 0.0, -10),
                                  (brilho, 0.05, -12)))]


# ---------------------------------------------------------------- CIDADE (dia e noite)

DIA = ('Sonniss.com-GDC2026-GameAudioBundle4of5/Sonik Sound Library - Urban Life/'
       'AMBUrbn_Ambience, City, Madrid, Spain, Soft Traffic and Human Activity, Some Birds-Surround_KSL_KS016.wav')


@receita('Ambiente', None, ogg=True, integrado_alvo=-30)
def amb_day():
    return [laco(trecho_estavel(S(DIA, True), 26.0), 2.0)]


@receita('Ambiente', None, ogg=True, integrado_alvo=-31)
def amb_night():
    grilos = seg(G('Animals_Nature_Ambiences/crickets_chirping_night_ambience_loop.wav', True), 0.0, 26.0)
    cidade = passa_baixa(trecho_estavel(S(DIA, True), 26.0), 500)
    return [laco(misturar((grilos, 0, 0), (cidade, 0, -12), estereo_saida=True), 2.0)]


@receita('Ambiente', -30)
def amb_cricket():
    return [aparar_cauda(G(f'Animals_Nature_Ambiences/cricket_chirping_solo_0{i}.wav')) for i in range(1, 5)]


# ----------------------------------------------------------------

def montar(nome):
    r = RECEITAS[nome]
    ATUAL[0] = nome
    variacoes = r['f']()
    feitos = []
    for i, x in enumerate(variacoes, 1):
        x = passa_alta(x, 25)
        if celular(x) < 25 and r['pasta'] != 'Ambiente':
            x = presenca(x)
        x = nivelar(x, r['alvo'], r['teto'], r['integrado'])
        if r['destino']:
            destino = ENERGIA / (r['destino'] + '.wav')
        else:
            ext = '.ogg' if r['ogg'] else '.wav'
            destino = V5 / r['pasta'] / f'{nome}_{i:02d}{ext}'
        gravar(x, destino, ogg=r['ogg'])
        extra = f' {integrado(x):6.1f} LUFS int' if r['integrado'] else ''
        feitos.append(f'{destino.relative_to(RAIZ)}  {len(x) / SR:5.2f}s pico {20 * np.log10(np.abs(x).max()):5.1f}{extra}')
    return feitos


if __name__ == '__main__':
    pedidos = sys.argv[1:] or list(RECEITAS)
    for nome in pedidos:
        for linha in montar(nome):
            print(linha, flush=True)
    creditos = RAIZ / 'Tools' / 'Audio' / 'creditos_sons.json'
    antigos = json.loads(creditos.read_text('utf-8')) if creditos.exists() else {}
    antigos.update(fontes_usadas)
    creditos.write_text(json.dumps(antigos, ensure_ascii=False, indent=1), 'utf-8')
