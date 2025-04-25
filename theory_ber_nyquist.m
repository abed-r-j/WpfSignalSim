close all;
clear all;

load('channelResponse.mat')

% === PARAMETERS ===
symbolRate = 32e9;                   % 70 Gbaud
bitsPerSymbol_N = 1;                 % NRZ
bitsPerSymbol_P = 2;                 % PAM4
sampleInterval = 6.25e-12;           % 6.25 ps
fs = 1 / sampleInterval;             % Sampling frequency (Hz)
samplesPerSymbol = round(fs / symbolRate);  % Typically ~14.3 → round to 14
numSymbols = 1000;                   % Number of symbols to simulate

% === WAVEFORM GENERATION ===

% 1. NRZ waveform
bits_N = randi([0 1], numSymbols * bitsPerSymbol_N, 1);
symbols_N = 2 * bits_N - 1;  % Map 0 → -1, 1 → +1
waveform_N = repelem(symbols_N, samplesPerSymbol);  % Upsample

% 2. PAM4 waveform
bits_P = randi([0 1], numSymbols * bitsPerSymbol_P, 1);
symbols_P = reshape(bits_P, [], 2);  % Group bits into 2s
% Map to PAM4 levels: 00→-3, 01→-1, 10→+1, 11→+3
pam4_levels = [-3 -1 +3 +1];
indices = symbols_P(:,1)*2 + symbols_P(:,2) + 1;
symbols_P_mapped = pam4_levels(indices);
waveform_P = repelem(symbols_P_mapped, samplesPerSymbol);  % Upsample

% === CHANNEL RESPONSE ===
% Assume ImpulseResponse and SampleInterval are already loaded

% Normalize impulse response energy
% ImpulseResponse = ImpulseResponse(:);
% ImpulseResponse = ImpulseResponse / sum(abs(ImpulseResponse));




% Filter NRZ and PAM4 waveforms
% output_N = conv(waveform_N, ImpulseResponse, 'same');
% output_P = conv(waveform_P, ImpulseResponse, 'same');

% Chaine de Nyquist
output_P=waveform_P;
output_N=waveform_N;

% Add noise
SNR_dB=30;
output_N = awgn(output_N, SNR_dB, 'measured');
output_P = awgn(output_P, SNR_dB, 'measured');


% === TIME AXIS ===
t = (0:length(output_N)-1) * sampleInterval * 1e9; % in ns

% === PLOTS ===

figure;
subplot(2,1,1);
plot(t, output_N);
title('Channel Output - NRZ');
xlabel('Time (ns)');
ylabel('Amplitude');

subplot(2,1,2);
plot(t, output_P);
title('Channel Output - PAM4');
xlabel('Time (ns)');
ylabel('Amplitude');

% % === EYE DIAGRAMS ===
% % For better eye diagrams, we need to:
% % 1. Use enough symbols (10,000+)
% % 2. Adjust the plotting window
% % 3. Possibly resample for better display
% 
% % Eye diagram for NRZ
% eyediagram(output_N, samplesPerSymbol, 2, samplesPerSymbol/2); 
% title('Eye Diagram - NRZ');
% 
% % Eye diagram for PAM4
% eyediagram(output_P, 2*samplesPerSymbol, 4, samplesPerSymbol/2);
% title('Eye Diagram - PAM4');


% === EYE DIAGRAMS WITH MID-SYMBOL SAMPLING ===
% Create time-shifted versions to sample at symbol center
time_offset = round(samplesPerSymbol/2); % Sample at middle of symbol

% NRZ Eye Diagram with sampling points
eyediagram(output_N, 2*samplesPerSymbol, 2, time_offset);
title('NRZ Eye Diagram');


% PAM4 Eye Diagram with sampling points
eyediagram(output_P, 2*samplesPerSymbol, 4, time_offset);
title('PAM4 Eye Diagram');


% === CONSTELLATION DIAGRAMS ===
% Take every samplesPerSymbol-th sample (symbol-rate sampling)
sampled_N = output_N(1:samplesPerSymbol:end);
sampled_P = output_P(1:samplesPerSymbol:end);

% Constellation for NRZ (PAM2)
figure;
plot(sampled_N, zeros(size(sampled_N)), 'bo', 'MarkerSize', 4, 'LineWidth', 1.5);
title('NRZ (PAM2) Constellation');
xlabel('In-Phase');
ylabel('Quadrature');  % Will be zero for 1D modulation
grid on;
axis([-1.5 1.5 -0.1 0.1]);  % Adjust axis for clarity

% Constellation for PAM4
figure;
plot(sampled_P, zeros(size(sampled_P)), 'ro', 'MarkerSize', 4, 'LineWidth', 1.5);
title('PAM4 Constellation');
xlabel('In-Phase');
ylabel('Quadrature');  % Will be zero for 1D modulation
grid on;
axis([-3.5 3.5 -0.1 0.1]);  % Adjust axis for PAM4 levels


% === VISUALIZE 10 TRANSMITTED & RECEIVED SYMBOLS ===
num_show = 10; % Number of symbols to display

% --- For NRZ (PAM2) ---
% Get transmitted and received symbols (downsampled to symbol rate)
tx_symbols_N = symbols_N(1:num_show);
rx_symbols_N = output_N(round(samplesPerSymbol/2):samplesPerSymbol:end); % Mid-symbol sampling
rx_symbols_N = rx_symbols_N(1:num_show);

figure;
subplot(2,1,1);
stem(tx_symbols_N, 'filled', 'LineWidth', 1.5, 'MarkerSize', 6);
title('NRZ: 10 Transmitted Symbols');
xlabel('Symbol Index');
ylabel('Amplitude');
ylim([-1.5 1.5]);
grid on;

subplot(2,1,2);
stem(rx_symbols_N, 'filled', 'LineWidth', 1.5, 'MarkerSize', 6, 'Color', [0.8 0 0]);
title('NRZ: 10 Received Symbols (After Channel + Noise)');
xlabel('Symbol Index');
ylabel('Amplitude');
ylim([-1.5 1.5]);
grid on;

% --- For PAM4 ---
tx_symbols_P = symbols_P_mapped(1:num_show);
rx_symbols_P = output_P(round(samplesPerSymbol/2):samplesPerSymbol:end); % Mid-symbol sampling
rx_symbols_P = rx_symbols_P(1:num_show);

figure;
subplot(2,1,1);
stem(tx_symbols_P, 'filled', 'LineWidth', 1.5, 'MarkerSize', 6);
title('PAM4: 10 Transmitted Symbols');
xlabel('Symbol Index');
ylabel('Amplitude');
ylim([-3.5 3.5]);
grid on;

subplot(2,1,2);
stem(rx_symbols_P, 'filled', 'LineWidth', 1.5, 'MarkerSize', 6, 'Color', [0.8 0 0]);
title('PAM4: 10 Received Symbols (After Channel + Noise)');
xlabel('Symbol Index');
ylabel('Amplitude');
ylim([-3.5 3.5]);
grid on;

%% performance

% === PARAMETRES BER ===
EbN0_dB_range = 0:2:16; % Plage de Eb/N0 en dB
numBits = 1e6;          % Nombre de bits pour la simulation BER
ber_NRZ = zeros(size(EbN0_dB_range));
ber_PAM4 = zeros(size(EbN0_dB_range));

% Boucle sur les valeurs de Eb/N0
for i = 1:length(EbN0_dB_range)
    EbN0_dB = EbN0_dB_range(i);
    
    % === NRZ (PAM2) Simulation ===
    bits_N = randi([0 1], numBits, 1);
    symbols_N = 2*bits_N - 1; % Mapping: 0->-1, 1->1
    
    % Ajout du bruit
    EbN0_linear = 10^(EbN0_dB/10);
    noise_var_N = 1/(2*EbN0_linear); % Variance du bruit
    received_N = symbols_N + sqrt(noise_var_N)*randn(size(symbols_N));
    
    % Détection
    detected_N = received_N > 0;
    ber_NRZ(i) = sum(bits_N ~= detected_N)/numBits;
    
    % === PAM4 Simulation ===
    bits_P = randi([0 1], 2*numBits, 1); % 2 bits par symbole
    symbols_P = reshape(bits_P, [], 2);
    pam4_levels = [-3 -1 1 3]; % Mapping Gray
    indices = bi2de(symbols_P, 'left-msb') + 1;
    symbols_P_mapped = pam4_levels(indices)';
    
    % Ajout du bruit (Eb/N0 par bit)
    noise_var_P = (5/(4*EbN0_linear)); % Variance adaptée pour PAM4
    received_P = symbols_P_mapped + sqrt(noise_var_P)*randn(size(symbols_P_mapped));
    
    % Détection PAM4
    thresholds = [-2 0 2];
    detected_P = zeros(size(received_P));
    for k = 1:length(received_P)
        if received_P(k) < thresholds(1)
            detected_P(k) = 0;
        elseif received_P(k) < thresholds(2)
            detected_P(k) = 1;
        elseif received_P(k) < thresholds(3)
            detected_P(k) = 2;
        else
            detected_P(k) = 3;
        end
    end
    bits_P_detected = de2bi(detected_P, 2, 'left-msb');
    ber_PAM4(i) = sum(bits_P ~= bits_P_detected(:))/(2*numBits);
end

% === Courbes théoriques ===
EbN0_linear = 10.^(EbN0_dB_range/10);
ber_NRZ_theo = qfunc(sqrt(2*EbN0_linear)); % Théorique NRZ
ber_PAM4_theo = (3/4)*qfunc(sqrt((4/5)*EbN0_linear)); % Théorique PAM4

% === Tracé des résultats ===
figure;
semilogy(EbN0_dB_range, ber_NRZ, 'bo-', 'LineWidth', 2, 'MarkerSize', 8);
hold on;
semilogy(EbN0_dB_range, ber_NRZ_theo, 'b--', 'LineWidth', 2);
semilogy(EbN0_dB_range, ber_PAM4, 'ro-', 'LineWidth', 2, 'MarkerSize', 8);
semilogy(EbN0_dB_range, ber_PAM4_theo, 'r--', 'LineWidth', 2);
grid on;
xlabel('Eb/N0 (dB)');
ylabel('Bit Error Rate (BER)');
title('Comparaison des BER simulés et théoriques');
legend('NRZ simulé', 'NRZ théorique', 'PAM4 simulé', 'PAM4 théorique');
axis([0 16 1e-6 1]);