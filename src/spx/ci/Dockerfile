FROM mcr.microsoft.com/mirror/docker/library/ubuntu:20.04 as output
ARG SPX_Version=1.25.0
ENV DEBIAN_FRONTEND=noninteractive

RUN echo SPX_Version=${SPX_Version} && \
    apt-get update && \
    apt-get install --yes wget unzip && \
    wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install --yes dotnet-runtime-6.0 \
        dotnet-sdk-6.0 && \
    apt-get install -y libglib2.0 &&\
    apt-get install -y libglib2.0-cil &&\
    apt-get install -y libglib2.0-cil-dev &&\
    apt-get install -y --no-install-recommends \
        python3 python3-pip python3-dev \
        libgirepository1.0 libasound2 \
        libgstreamer1.0-0 gstreamer1.0-plugins-base gstreamer1.0-plugins-good gstreamer1.0-plugins-bad gstreamer1.0-plugins-ugly python3-gst-1.0 \
        apt-transport-https

RUN wget -q -O - https://aka.ms/InstallAzureCLIDeb | bash && \
    python3 --version && \
    python3 -m pip install setuptools wheel && \
    python3 -m pip install deepdiff==5.7.0 && \
    python3 -m pip install batchkit-examples-speechsdk==0.9.9.dev0 && \
    mkdir /batchkit_config && \
    echo "[]" > /batchkit_config/config.yaml

COPY ./downloads/zips /downloads/zips

RUN dotnet tool install --global Microsoft.CognitiveServices.Speech.CLI \
        --version ${SPX_Version} \
        --add-source /downloads/zips/

ENV PATH=${PATH}:"/root/.dotnet/tools/"

# cleanup unneeded files to reduce image size.
RUN rm -r /downloads && \
    set -ex apt-get autoremove -y && \
    apt-get clean -y && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /data

ENTRYPOINT ["spx"]

CMD ["recognize", "@defaults", "--files", "*.wav"]
