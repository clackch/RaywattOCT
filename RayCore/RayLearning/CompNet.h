#pragma once
#include <torch/torch.h>

#define USE_REGISTER_MODULE // register_module() is needed if we want to use the parameters() method later on

using namespace std;

/*
* model for Lumen Extraction
*/
struct SELayerImpl : torch::nn::Module {
    torch::nn::AdaptiveAvgPool2d avg_pool = nullptr;
    torch::nn::Sequential fc = nullptr;

    SELayerImpl(int ch, int reduction = 16) {
        avg_pool = torch::nn::AdaptiveAvgPool2d(torch::nn::AdaptiveAvgPool2dOptions(1));
        fc = torch::nn::Sequential(
            torch::nn::Linear(torch::nn::LinearOptions(ch, ch / reduction).bias(false)),
            torch::nn::ReLU(torch::nn::ReLUOptions(true)),
            torch::nn::Linear(torch::nn::LinearOptions(ch / reduction, ch).bias(false)),
            torch::nn::Sigmoid());

#ifdef USE_REGISTER_MODULE
        register_module("avg_pool", avg_pool);
        register_module("fc", fc);
#endif
    }

    torch::Tensor forward(torch::Tensor x) {
        c10::IntArrayRef sizes = x.sizes();
        torch::Tensor y = avg_pool(x).view({ sizes[0], sizes[1] });
        y = fc->forward(y).view({ sizes[0], sizes[1], 1, 1 });

        return x * y.expand_as(x);
    }
};
TORCH_MODULE(SELayer);

struct ResidualBlockImpl : torch::nn::Module {
    torch::nn::Conv2d conv1 = nullptr, conv2 = nullptr, conv3 = nullptr;
    torch::nn::BatchNorm2d bn1 = nullptr, bn2 = nullptr, bn3 = nullptr;
    SELayer se;
    torch::nn::ReLU relu = nullptr;

    ResidualBlockImpl(int in_ch, int out_ch)
        : se(SELayer(out_ch)) {
        conv1 = torch::nn::Conv2d(torch::nn::Conv2dOptions(in_ch, out_ch, 3).padding(1));
        bn1 = torch::nn::BatchNorm2d(torch::nn::BatchNorm2dOptions(out_ch));

        conv2 = torch::nn::Conv2d(torch::nn::Conv2dOptions(out_ch, out_ch, 3).padding(1));
        bn2 = torch::nn::BatchNorm2d(torch::nn::BatchNorm2dOptions(out_ch));

        conv3 = torch::nn::Conv2d(torch::nn::Conv2dOptions(in_ch, out_ch, 1).padding(0));
        bn3 = torch::nn::BatchNorm2d(torch::nn::BatchNorm2dOptions(out_ch));

        relu = torch::nn::ReLU(torch::nn::ReLUOptions().inplace(true));

#ifdef USE_REGISTER_MODULE
        register_module("conv1", conv1);
        register_module("conv2", conv2);
        register_module("conv3", conv3);
        register_module("bn1", bn1);
        register_module("bn2", bn2);
        register_module("bn3", bn3);
        register_module("se", se);
        register_module("relu", relu);
#endif
    }

    torch::Tensor forward(torch::Tensor x) {
        torch::Tensor x1, x2, x3, x4;

        x1 = conv1(x);
        x1 = bn1(x1);
        x1 = relu(x1);

        x2 = conv2(x1);
        x2 = bn2(x2);

        x3 = conv3(x);
        x3 = bn3(x3);
        x3 = se(x3);

        x4 = x2 + x3;
        x4 = relu(x4);

        return x4;
    }
};
TORCH_MODULE(ResidualBlock);

struct EncoderBlockImpl : torch::nn::Module {
    ResidualBlock r1, r2;
    torch::nn::MaxPool2d pool = nullptr;

    EncoderBlockImpl(int in_ch, int out_ch) :
        r1(ResidualBlock(in_ch, out_ch)),
        r2(ResidualBlock(out_ch, out_ch))
    {
        pool = torch::nn::MaxPool2d(torch::nn::MaxPool2dOptions(2).stride(2));

#ifdef USE_REGISTER_MODULE
        register_module("r1", r1);
        register_module("r2", r2);
        register_module("pool", pool);
#endif
    }

    tuple<torch::Tensor, torch::Tensor> forward(torch::Tensor x) {
        x = r1(x);
        x = r2(x);
        torch::Tensor p = pool(x);

        return tuple<torch::Tensor, torch::Tensor>(x, p);
    }
};
TORCH_MODULE(EncoderBlock);

struct DecoderBlockImpl : torch::nn::Module {
    torch::nn::ConvTranspose2d upsample = nullptr;
    ResidualBlock r1, r2;

    DecoderBlockImpl(int in_ch, int out_ch) :
        r1(ResidualBlock(in_ch + out_ch, out_ch)),
        r2(ResidualBlock(out_ch, out_ch))
    {
        upsample = torch::nn::ConvTranspose2d(torch::nn::ConvTranspose2dOptions(in_ch, out_ch, 4).stride(2).padding(1));

#ifdef USE_REGISTER_MODULE
        register_module("upsample", upsample);
        register_module("r1", r1);
        register_module("r2", r2);
#endif
    }

    torch::Tensor forward(torch::Tensor x, torch::Tensor s) {
        x = upsample(x);
        x = torch::cat({ x, s }, 1);
        x = r1(x);
        x = r2(x);

        return x;
    }
};
TORCH_MODULE(DecoderBlock);

struct CompNetImpl : torch::nn::Module {
    EncoderBlock e1, e2, e3, e4;
    DecoderBlock s1, s2, s3, s4;
    DecoderBlock a1, a2, a3, a4;
    torch::nn::Sequential m1 = nullptr, m2 = nullptr, m3 = nullptr, m4 = nullptr;
    torch::nn::Conv2d output1 = nullptr, output2 = nullptr;

    CompNetImpl() :
        e1(EncoderBlock(3, 32)),
        e2(EncoderBlock(32, 64)),
        e3(EncoderBlock(64, 128)),
        e4(EncoderBlock(128, 256)),
        s1(DecoderBlock(256, 128)),
        s2(DecoderBlock(128, 64)),
        s3(DecoderBlock(64, 32)),
        s4(DecoderBlock(32, 16)),
        a1(DecoderBlock(256, 128)),
        a2(DecoderBlock(128, 64)),
        a3(DecoderBlock(64, 32)),
        a4(DecoderBlock(32, 16))
    {
        m1 = torch::nn::Sequential(
            torch::nn::Conv2d(torch::nn::Conv2dOptions(128, 1, 1).padding(0)),
            torch::nn::Sigmoid()
        );
        m2 = torch::nn::Sequential(
            torch::nn::Conv2d(torch::nn::Conv2dOptions(64, 1, 1).padding(0)),
            torch::nn::Sigmoid()
        );
        m3 = torch::nn::Sequential(
            torch::nn::Conv2d(torch::nn::Conv2dOptions(32, 1, 1).padding(0)),
            torch::nn::Sigmoid()
        );
        m4 = torch::nn::Sequential(
            torch::nn::Conv2d(torch::nn::Conv2dOptions(16, 1, 1).padding(0)),
            torch::nn::Sigmoid()
        );

        output1 = torch::nn::Conv2d(torch::nn::Conv2dOptions(16, 1, 1).padding(0));
        output2 = torch::nn::Conv2d(torch::nn::Conv2dOptions(16, 1, 1).padding(0));

#ifdef USE_REGISTER_MODULE
        register_module("e1", e1);
        register_module("e2", e2);
        register_module("e3", e3);
        register_module("e4", e4);
        register_module("s1", s1);
        register_module("s2", s2);
        register_module("s3", s3);
        register_module("s4", s4);
        register_module("a1", a1);
        register_module("a2", a2);
        register_module("a3", a3);
        register_module("a4", a4);
        register_module("m1", m1);
        register_module("m2", m2);
        register_module("m3", m3);
        register_module("m4", m4);
        register_module("output1", output1);
        register_module("output2", output2);
#endif
    }

    tuple<torch::Tensor, torch::Tensor> forward(torch::Tensor x) {
        torch::Tensor x1, p1;
        torch::Tensor x2, p2;
        torch::Tensor x3, p3;
        torch::Tensor x4, p4;
        tuple<torch::Tensor, torch::Tensor> encoderResult;

        encoderResult = e1(x);
        x1 = get<0>(encoderResult);
        p1 = get<1>(encoderResult);

        encoderResult = e2(p1);
        x2 = get<0>(encoderResult);
        p2 = get<1>(encoderResult);

        encoderResult = e3(p2);
        x3 = get<0>(encoderResult);
        p3 = get<1>(encoderResult);

        encoderResult = e4(p3);
        x4 = get<0>(encoderResult);
        p4 = get<1>(encoderResult);

        torch::Tensor seg1 = s1(p4, x4);
        torch::Tensor auto1 = a1(p4, x4);
        torch::Tensor map1 = m1.get()->forward(auto1);
        torch::Tensor x5 = seg1 * map1;

        torch::Tensor seg2 = s2(x5, x3);
        torch::Tensor auto2 = a2(auto1, x3);
        torch::Tensor map2 = m2.get()->forward(auto2);
        torch::Tensor x6 = seg2 * map2;

        torch::Tensor seg3 = s3(x6, x2);
        torch::Tensor auto3 = a3(auto2, x2);
        torch::Tensor map3 = m3.get()->forward(auto3);
        torch::Tensor x7 = seg3 * map3;

        torch::Tensor seg4 = s4(x7, x1);
        torch::Tensor auto4 = a4(auto3, x1);
        torch::Tensor map4 = m4.get()->forward(auto4);
        torch::Tensor x8 = seg4 * map4;

        torch::Tensor out1 = output1(x8);
        torch::Tensor out2 = output2(auto4);

        return tuple<torch::Tensor, torch::Tensor>(out1, out2);
    }
};
TORCH_MODULE(CompNet);